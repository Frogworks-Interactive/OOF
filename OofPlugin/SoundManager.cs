using NAudio.Wave;
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace OofPlugin;

internal class SoundManager : IDisposable {
  private readonly Configuration Configuration;
  private readonly DeadPlayersList DeadPlayersList;

  // sound
  public bool isSoundPlaying { get; private set; } = false;
  private DirectSoundOut? soundOut;
  private string? soundFile;

  internal CancellationTokenSource CancelToken;

  public SoundManager(OofPlugin plugin) {
    Configuration = plugin.Configuration;
    DeadPlayersList = plugin.DeadPlayersList;

    LoadFile();

    CancelToken = new CancellationTokenSource();
    Task.Run(() => OofAudioPolling(CancelToken.Token));
  }

  public void LoadFile() {
    if (string.IsNullOrEmpty(Configuration.DefaultSoundImportPath)) {
      soundFile = Path.Combine(
          Dalamud.PluginInterface.AssemblyLocation.Directory!.FullName,
          "oof.wav");
      return;
    }

    soundFile = Configuration.DefaultSoundImportPath;
  }

  public void Stop() {
    soundOut?.Pause();
    soundOut?.Dispose();
    soundOut = null;
  }

  public void Play(CancellationToken token, float volume = 1f) {
    Task.Run(() => {
      isSoundPlaying = true;

      WaveStream reader;
      try {
        reader = new MediaFoundationReader(soundFile);
      }
      catch (Exception ex) {
        isSoundPlaying = false;
        Dalamud.Log.Error("Failed to read sound file", ex);
        return;
      }

      var audioStream =
          new WaveChannel32(reader) {
            Volume = Configuration.Volume * volume,
            PadWithZeroes = false
          };

      using (reader) {
        soundOut?.Dispose();
        soundOut = new DirectSoundOut();

        try {
          soundOut.Init(audioStream);
          soundOut.Play();

          soundOut.PlaybackStopped += (_, _) => { isSoundPlaying = false; };
        }
        catch (Exception ex) {
          isSoundPlaying = false;
          Dalamud.Log.Error("Failed to play sound", ex);
        }
      }
    }, token);
  }

  private async Task OofAudioPolling(CancellationToken token) {
    while (!token.IsCancellationRequested) {
      try {
        await Task.Delay(200, token);

        if (DeadPlayersList == null || DeadPlayersList.DeadPlayers.Count == 0)
          continue;

        // Run on framework thread AND await it so exceptions are observed
        await Dalamud.Framework.RunOnFrameworkThread(() => {
          var localPlayer =
              Dalamud.ObjectTable.LocalPlayer;
          if (localPlayer is null)
            return;

          foreach (var player in DeadPlayersList.DeadPlayers) {
            if (player.DidPlayOof)
              continue;

            float volume = 1f;

            if (Configuration.DistanceBasedOof &&
                player.Distance != Vector3.Zero) {
              var dist =
                  Vector3.Distance(localPlayer.Position, player.Distance);
              volume = VolumeFromDist(dist);
            }

            Play(token, volume);
            player.DidPlayOof = true;
            break;
          }
        });
      }
      catch (OperationCanceledException) {
        // normal shutdown
        break;
      }
      catch (Exception ex) {
        Dalamud.Log.Error(ex, "OOF: OofAudioPolling crashed");
        // keep loop alive instead of dying forever
      }
    }
  }

  public float VolumeFromDist(float dist, float distMax = 30f) {
    dist = Math.Min(dist, distMax);

    var falloff = Configuration.DistanceFalloff > 0
                      ? 3f - Configuration.DistanceFalloff * 3f
                      : 2.999f;

    var vol = 1f - ((dist / distMax) * (1f / falloff));
    return Math.Max(Configuration.DistanceMinVolume, vol);
  }

  public async Task TestDistanceAudio(CancellationToken token) {
    async Task PlayTest(float volume) {
      if (token.IsCancellationRequested)
        return;

      Play(token, volume);
      await Task.Delay(700, token);
    }

    await PlayTest(VolumeFromDist(0));
    await PlayTest(VolumeFromDist(10));
    await PlayTest(VolumeFromDist(20));
    await PlayTest(VolumeFromDist(30));
  }

  public void Dispose() {
    CancelToken.Cancel();
    CancelToken.Dispose();

    soundOut?.Dispose();
    soundOut = null;
  }
}

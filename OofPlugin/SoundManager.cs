using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace OofPlugin;

internal class SoundManager : IDisposable
{
    Configuration Configuration { get; set; }

    DeadPlayersList DeadPlayersList { get; init; }

    // sound
    public bool isSoundPlaying { get; set; } = false;
    private DirectSoundOut? soundOut;
    private string? soundFile { get; set; }

    internal CancellationTokenSource CancelToken;

    public SoundManager(OofPlugin plugin)
    {
        Configuration = plugin.Configuration;
        DeadPlayersList = plugin.DeadPlayersList;
        LoadFile();

        // lmao
        CancelToken = new CancellationTokenSource();
        Task.Run(() => OofAudioPolling(CancelToken.Token));

    }
    public void LoadFile()
    {
        if (Configuration.DefaultSoundImportPath.Length == 0)
        {
            var path = Path.Combine(OofPlugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.wav");
            soundFile = path;
            return;
        }
        soundFile = Configuration.DefaultSoundImportPath;
    }

    public void Stop()
    {
        soundOut?.Pause();
        soundOut?.Dispose();

    }
    /// <summary>
    /// Play sound but without referencing windows.forms.
    /// much of the code from: https://github.com/kalilistic/Tippy/blob/5c18d6b21461b0bbe4583a86787ef4a3565e5ce6/src/Tippy/Tippy/Logic/TippyController.cs#L11
    /// </summary>
    /// <param name="token">cancellation token</param>
    /// <param name="volume">optional volume param</param>
    public void Play(CancellationToken token, float volume = 1)
    {
        Task.Run(() =>
        {
            isSoundPlaying = true;
            WaveStream reader;
            try
            {
                reader = new MediaFoundationReader(soundFile);
            }
            catch (Exception ex)
            {
                isSoundPlaying = false;
                OofPlugin.PluginLog.Error("Failed read file", ex);
                return;
            }

            var audioStream = new WaveChannel32(reader)
            {
                Volume = Configuration.Volume * volume,
                PadWithZeroes = false // you need this or else playbackstopped event will not fire
            };
            using (reader)
            {
                if (isSoundPlaying && soundOut != null)
                {
                    soundOut.Pause();
                    soundOut.Dispose();
                };
                //shoutout anna clemens for the winforms fix
                soundOut = new DirectSoundOut();

                try
                {
                    soundOut.Init(audioStream);
                    soundOut.Play();
                    soundOut.PlaybackStopped += OnPlaybackStopped;
                    // run after sound has played. does this work? i have no idea
                    void OnPlaybackStopped(object? sender, StoppedEventArgs e)
                    {
                        soundOut.PlaybackStopped -= OnPlaybackStopped;
                        isSoundPlaying = false;
                    }
                }
                catch (Exception ex)
                {
                    isSoundPlaying = false;
                    OofPlugin.PluginLog.Error("Failed play sound", ex);
                    return;
                }
            }

        }, token);
    }



    /// <summary>
    /// check deadPlayers every once in a while. prevents multiple oof from playing too fast
    /// </summary>
    /// <param name="token"> cancellation token</param>
    private async Task OofAudioPolling(CancellationToken token)
    {
        while (true)
        {
            await Task.Delay(200, token);
            if (token.IsCancellationRequested) break;
            if (!DeadPlayersList.DeadPlayers.Any()) continue;
            if (OofPlugin.ClientState!.LocalPlayer! == null) continue;
            foreach (var player in DeadPlayersList.DeadPlayers)
            {
                if (player.DidPlayOof) continue;
                float volume = 1f;
                if (Configuration.DistanceBasedOof && player.Distance != OofPlugin.ClientState!.LocalPlayer!.Position)
                {
                    var dist = 0f;
                    if (player.Distance != Vector3.Zero) dist = Vector3.Distance(OofPlugin.ClientState!.LocalPlayer!.Position, player.Distance);
                    volume = VolumeFromDist(dist);
                }
                Play(token, volume);
                player.DidPlayOof = true;
                break;

            }
        }
    }
    public float VolumeFromDist(float dist, float distMax = 30)
    {
        if (dist > distMax) dist = distMax;
        var falloff = Configuration.DistanceFalloff > 0 ? 3f - Configuration.DistanceFalloff * 3f : 3f - 0.001f;
        var vol = 1f - ((dist / distMax) * (1 / falloff));
        return Math.Max(Configuration.DistanceMinVolume, vol);
    }
    public async Task TestDistanceAudio(CancellationToken token)
    {
        async Task CheckthenPlay(float volume)
        {
            if (token.IsCancellationRequested) return;

            Play(token, volume);
            await Task.Delay(700, token);
        }
        await CheckthenPlay(VolumeFromDist(0));
        await CheckthenPlay(VolumeFromDist(10));
        await CheckthenPlay(VolumeFromDist(20));
        await CheckthenPlay(VolumeFromDist(30));

    }

    public void Dispose()
    {
        CancelToken.Cancel();
        CancelToken.Dispose();

        try
        {
            while (isSoundPlaying)
            {
                Thread.Sleep(100);
                soundOut?.Pause();
                isSoundPlaying = false;

            }
            soundOut?.Dispose();
        }
        catch (Exception e)
        {
            OofPlugin.PluginLog.Error("Failed to dispose oofplugin controller", e.Message);
        }
    }
}


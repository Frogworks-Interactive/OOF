using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using NAudio.Wave;
using OofPlugin.Windows;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Task = System.Threading.Tasks.Task;

//shoutout anna clemens

namespace OofPlugin
{
    public sealed class OofPlugin : IDalamudPlugin
    {
        public string Name => "OOF";

        private const string oofCommand = "/oof";
        private const string oofSettings = "/oofsettings";
        private const string oofVideo = "/oofvideo";
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;
        [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("OofPlugin");
        private ConfigWindow ConfigWindow { get; init; }
        private DeadPlayersList DeadPlayersList { get; init; }

        // i love global variables!!!! the more global the more globaly it gets
        // sound
        public bool isSoundPlaying { get; set; } = false;
        private DirectSoundOut? soundOut;
        private string? soundFile { get; set; }

        //check for fall
        private float prevPos { get; set; } = 0;
        private float prevVel { get; set; } = 0;
        private float distJump { get; set; } = 0;
        private bool wasJumping { get; set; } = false;

        public CancellationTokenSource CancelToken;

        public OofPlugin()
        {
            DeadPlayersList = new DeadPlayersList();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            CommandManager.AddHandler(oofCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "play oof sound"
            });
            CommandManager.AddHandler(oofSettings, new CommandInfo(OnCommand)
            {
                HelpMessage = "change oof settings"
            });
            CommandManager.AddHandler(oofVideo, new CommandInfo(OnCommand)
            {
                HelpMessage = "open Hbomberguy video on OOF.mp3"
            });

            // load audio file. idk if this the best way
            LoadSoundFile();

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            Framework.Update += FrameworkOnUpdate;

            // lmao
            CancelToken = new CancellationTokenSource();
            Task.Run(() => OofAudioPolling(CancelToken.Token));

        }

        private void OnCommand(string command, string args)
        {
            if (command == oofCommand) PlaySound(CancelToken.Token);
            if (command == oofSettings) ToggleConfigUI();
            if (command == oofVideo) OpenVideo();

        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI()
        {
            ConfigWindow.Toggle();
        }
        private void FrameworkOnUpdate(IFramework framework)
        {
            if (ClientState == null || ClientState.LocalPlayer == null) return;
            // dont run btwn moving areas          
            if (Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] || Condition[ConditionFlag.BeingMoved]) return;
            if (Condition[ConditionFlag.WatchingCutscene] || Condition[ConditionFlag.WatchingCutscene78]) return;


            try
            {
                if (Configuration.OofOnFall) CheckFallen();
                if (Configuration.OofOnDeath) CheckDeath();
            }
            catch (Exception e)
            {
                PluginLog.Error("failed to check for oof condition:", e.Message);
            }
        }

        /// <summary>
        /// check if player has died during alliance, party, and self.
        /// this may be the worst if statement chain i have made
        /// </summary>
        private void CheckDeath()
        {

            if (!Configuration.OofOnDeathBattle && Condition[ConditionFlag.InCombat]) return;

            if (PartyList != null && PartyList.Any())
            {
                if (Configuration.OofOnDeathAlliance && PartyList.Length == 8 && PartyList.GetAllianceMemberAddress(0) != IntPtr.Zero) // the worst "is alliance" check
                {
                    try
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            var allianceMemberAddress = PartyList.GetAllianceMemberAddress(i);
                            if (allianceMemberAddress == IntPtr.Zero) throw new NullReferenceException("allience member address is null");

                            var allianceMember = PartyList.CreateAllianceMemberReference(allianceMemberAddress) ?? throw new NullReferenceException("allience reference is null");
                            DeadPlayersList.AddRemoveDeadPlayer(allianceMember);
                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error("failed alliance check", e.Message);
                    }
                }
                if (Configuration.OofOnDeathParty)
                {
                    foreach (var member in PartyList)
                    {
                        DeadPlayersList.AddRemoveDeadPlayer(member, member.Territory.Id == ClientState.TerritoryType);
                    }
                }

            }
            else
            {
                if (Configuration.OofOnDeathSelf) return;
                DeadPlayersList.AddRemoveDeadPlayer(ClientState.LocalPlayer!);
            }

        }

        /// <summary>
        /// check if player has taken fall damage (brute force way)
        /// </summary>
        private void CheckFallen()
        {
            if (!Configuration.OofOnFallBattle && Condition[ConditionFlag.InCombat]) return;
            if (!Configuration.OofOnFallMounted && (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2])) return;

            var isJumping = Condition[ConditionFlag.Jumping];
            var pos = ClientState!.LocalPlayer!.Position.Y;
            var velocity = prevPos - pos;

            if (isJumping && !wasJumping)
            {
                if (prevVel < 0.17) distJump = pos; //started falling
            }
            else if (wasJumping && !isJumping)  // stopped falling
            {
                if (distJump - pos > 9.60) PlaySound(CancelToken.Token); // fell enough to take damage // i guessed and checked this distance value btw
            }

            // set position for next timestep
            prevPos = pos;
            prevVel = velocity;
            wasJumping = isJumping;
        }

        public void LoadSoundFile()
        {
            if (Configuration.DefaultSoundImportPath.Length == 0)
            {
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.wav");
                soundFile = path;
                return;
            }
            soundFile = Configuration.DefaultSoundImportPath;
        }

        public void StopSound()
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
        public void PlaySound(CancellationToken token, float volume = 1)
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
                    PluginLog.Error("Failed read file", ex);
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
                        PluginLog.Error("Failed play sound", ex);
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
                if (ClientState!.LocalPlayer! == null) continue;
                foreach (var player in DeadPlayersList.DeadPlayers)
                {
                    if (player.DidPlayOof) continue;
                    float volume = 1f;
                    if (Configuration.DistanceBasedOof && player.Distance != ClientState!.LocalPlayer!.Position)
                    {
                        var dist = 0f;
                        if (player.Distance != Vector3.Zero) dist = Vector3.Distance(ClientState!.LocalPlayer!.Position, player.Distance);
                        volume = CalcVolumeFromDist(dist);
                    }
                    PlaySound(token, volume);
                    player.DidPlayOof = true;
                    break;

                }
            }
        }
        public float CalcVolumeFromDist(float dist, float distMax = 30)
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

                PlaySound(token, volume);
                await Task.Delay(500, token);
            }
            await CheckthenPlay(CalcVolumeFromDist(0));
            await CheckthenPlay(CalcVolumeFromDist(10));
            await CheckthenPlay(CalcVolumeFromDist(20));
            await CheckthenPlay(CalcVolumeFromDist(30));

        }

        /// <summary>
        /// open the hbomberguy video on oof
        /// </summary>
        public static void OpenVideo()
        {
            Util.OpenLink("https://www.youtube.com/watch?v=0twDETh6QaI");
        }

        /// <summary>
        /// dispose
        /// </summary>
        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();

            CommandManager.RemoveHandler(oofCommand);
            CommandManager.RemoveHandler(oofSettings);
            CommandManager.RemoveHandler(oofVideo);
            CancelToken.Cancel();
            CancelToken.Dispose();

            Framework.Update -= FrameworkOnUpdate;
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
                PluginLog.Error("Failed to dispose oofplugin controller", e.Message);
            }


        }


    }
}
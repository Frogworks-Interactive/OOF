using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Objects;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;

//shoutout anna clemens

namespace OofPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "OOF";

        private const string oofCommand = "/oof";
        private const string oofSettings = "/oofsettings";
        private const string oofVideo = "/oofvideo";

        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static Condition Condition { get; private set; } = null!;
        [PluginService] public static PartyList PartyList { get; private set; } = null!;
        [PluginService] public static ObjectTable ObjectTable { get; private set; } = null!;

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        // i love global variables!!!! the more global the more globaly it gets
        // sound
        public bool isSoundPlaying { get; set; } = false;
        private WaveFileReader? reader;
        private DirectSoundOut? soundOut;
        private byte[] soundFile { get; set; }

        //check for fall
        public float prevPos { get; set; } = 0;
        private float prevVel { get; set; } = 0;
        public float distJump { get; set; } = 0;
        public bool wasJumping { get; set; } = false;
        public bool test { get; set; } = false;
        public Dictionary<uint,bool> deadPlayers { get; set; } = new Dictionary<uint, bool> { };

        public CancellationTokenSource CancelToken;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.PluginUi = new PluginUI(this.Configuration, this);

            // load audio file. idk if this the best way
            loadSoundFile();

            this.CommandManager.AddHandler(oofCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "play oof sound"
            });
            this.CommandManager.AddHandler(oofSettings, new CommandInfo(OnCommand)
            {
                HelpMessage = "change oof settings"
            });
            this.CommandManager.AddHandler(oofVideo, new CommandInfo(OnCommand)
            {
                HelpMessage = "open Hbomberguy video on OOF.mp3"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += this.FrameworkOnUpdate;

            // lmao
            CancelToken = new CancellationTokenSource();
            Task.Run(() => oofAudioPolling(CancelToken.Token));

        }
        public void loadSoundFile()
        {
            if (Configuration.DefaultSoundImportPath.Length == 0)
            {
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.wav");
                this.soundFile = File.ReadAllBytes(path);

                return;
            }
            this.soundFile = File.ReadAllBytes(Configuration.DefaultSoundImportPath);
        }
        private void OnCommand(string command, string args)
        {
            if (command == oofCommand) PlaySound();
            if (command == oofSettings) this.PluginUi.SettingsVisible = true;
            if (command == oofVideo) openVideo();

         }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
        private void FrameworkOnUpdate(Framework framework)
        {
            if (ClientState == null || ClientState.LocalPlayer == null) return;
            if (!Configuration.OofInBattle && Condition[ConditionFlag.InCombat]) return;
            try
            {
                if (Configuration.OofOnFall) CheckFallen();
                if (Configuration.OofOnDeath) CheckDeath();
            }
            catch (Exception e)
            {
                PluginLog.Error("failed to check for oof condition:", e);
            }
        }
      
        /// <summary>
        /// check if player has died
        /// </summary>
        private void CheckDeath()
        {
            if (Configuration.OofInBattle && PartyList != null && PartyList.Any())
            {
                if(PartyList.Length == 8 && PartyList.GetAllianceMemberAddress(0) != IntPtr.Zero) // the worst "is alliance" check
                {
                    try
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            var allianceMemberAddress = PartyList.GetAllianceMemberAddress(i);

                            if (allianceMemberAddress != IntPtr.Zero)
                            {
                                var allianceMember = PartyList.CreateAllianceMemberReference(allianceMemberAddress);
                                if (allianceMember.CurrentHP == 0 && !deadPlayers.ContainsKey(allianceMember.ObjectId))
                                {
                                    deadPlayers[allianceMember.ObjectId] = true;
                                }
                                else if (allianceMember.CurrentHP != 0 && deadPlayers.ContainsKey(allianceMember.ObjectId))
                                {
                                    deadPlayers.Remove(allianceMember.ObjectId);
                                }

                            }

                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.LogError("failed at alliance", e.Message);
                    }
                }
                  
                foreach (var member in PartyList)
                {
                    if (member.CurrentHP == 0 && !deadPlayers.ContainsKey(member.ObjectId) && member.Territory.Id == ClientState!.TerritoryType) {
                        deadPlayers[member.ObjectId] = true;
                    } else if (member.CurrentHP != 0 && deadPlayers.ContainsKey(member.ObjectId))
                    {
                        deadPlayers.Remove(member.ObjectId);
                    }
                }
            } else
            {
                if (ClientState!.LocalPlayer!.CurrentHp == 0 && !deadPlayers.ContainsKey(ClientState!.LocalPlayer!.ObjectId))
                {
                    deadPlayers[ClientState!.LocalPlayer!.ObjectId] = true;
                }
                else if (ClientState!.LocalPlayer!.CurrentHp != 0 && deadPlayers.ContainsKey(ClientState!.LocalPlayer!.ObjectId))
                {
                    deadPlayers.Remove(ClientState!.LocalPlayer!.ObjectId);
                }
            }
          
        }
        /// <summary>
        /// check if player has taken fall damage (brute force way)
        /// </summary>
        private void CheckFallen()
        {   
            // dont run if mounted
            if (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2] || Condition[ConditionFlag.InCombat]) return;
           
            var isJumping = Condition[ConditionFlag.Jumping];
            var pos = ClientState!.LocalPlayer!.Position.Y;
            var velocity = prevPos - pos;

            if (isJumping && !wasJumping)
            {
                if (prevVel < 0.17) distJump = pos; //started falling
            }
            else if (wasJumping && !isJumping)  // stopped falling
            {
                if (distJump - pos > 9.60) PlaySound(); // fell enough to take damage // i guessed and checked this distance value btw
            }

            // set position for next timestep
            prevPos = pos;
            prevVel = velocity;
            wasJumping = isJumping;
        }

        /// <summary>
        /// Play sound but without referencing windows.forms.
        /// i hope this doesnt leak memory
        /// </summary>
        /// <param name="volume">optional volume param</param>
        public void PlaySound(float volume = 1)
        {
            try
            {
                if (this.isSoundPlaying) this.soundOut!.Stop();
              
                var soundDevice = DirectSoundOut.Devices.FirstOrDefault();
                if (soundDevice == null)
                {
                    PluginLog.Error("no sound device lmao");
                    return;
                }

                this.isSoundPlaying = true;
                this.reader = new WaveFileReader(new MemoryStream(this.soundFile));

                var audioStream = new WaveChannel32(this.reader);
                audioStream.Volume = Configuration.Volume * volume;
                audioStream.PadWithZeroes = false; // you need this or else playbackstopped event will not fire
                //shoutout anna clemens for the winforms fix
                soundOut = new DirectSoundOut(soundDevice.Guid);
                soundOut.Init(audioStream);
                soundOut.Play();
                soundOut.PlaybackStopped += onPlaybackStopped;

            }
            catch (Exception e)
            {
                this.isSoundPlaying = false;
                PluginLog.Error(e,"failed to play oof sound");
            }
        }

        /// <summary>
        /// run after sound has played.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            this.soundOut!.PlaybackStopped -= this.onPlaybackStopped;
            this.isSoundPlaying = false;
        }

        /// <summary>
        /// open the hbomberguy video on oof
        /// </summary>
        public void openVideo()
        {
            ProcessStartInfo openVideo = new ProcessStartInfo
            {
                FileName = "https://www.youtube.com/watch?v=0twDETh6QaI",
                UseShellExecute = true
            };
            Process.Start(openVideo);
        }

        /// <summary>
        /// check deadPlayers every once in a while. prevents multiple oof from playing too fast
        /// </summary>
        /// <param name="token"> cancellation token</param>
        private void oofAudioPolling(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) break;
                Task.Delay(200).Wait();
                if (deadPlayers.Any())
                {
                    foreach (var player in deadPlayers)
                    {
                        if (player.Value)
                        {
                    
                            var playerObject = ObjectTable.SearchById(player.Key);
                            if (playerObject != null)
                            {
                                var localPlayerPos = ClientState!.LocalPlayer!.Position;
                               var distance = Vector3.Distance(localPlayerPos,playerObject.Position);
                            }
                            

                            PlaySound();
                            deadPlayers[player.Key] = false;
                            break;
                        }

                    }

                }
            }
        }


        /// <summary>
        /// dispose
        /// </summary>
        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(oofCommand);
            this.CommandManager.RemoveHandler(oofSettings);
            this.CommandManager.RemoveHandler(oofVideo);
            CancelToken.Cancel();
            CancelToken.Dispose();

            Framework.Update -= this.FrameworkOnUpdate;
            try
            {
                while (this.isSoundPlaying)
                {
                    Thread.Sleep(100);
                }
                this.isSoundPlaying = false ;

                this.reader!.Dispose();
                this.soundOut!.Dispose();
            }
            catch (Exception e)
            {
                PluginLog.LogError("Failed to dispose oofplugin controller", e);
            }


        }


    }
}

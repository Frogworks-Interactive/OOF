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
using Dalamud.Utility;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using static Lumina.Data.Parsing.Layer.LayerCommon;

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
       // private WaveStream? reader;
       // private DirectSoundOut? soundOut;
        private string soundFile { get; set; }

        //check for fall
        private float prevPos { get; set; } = 0;
        private float prevVel { get; set; } = 0;
        private float distJump { get; set; } = 0;
        private bool wasJumping { get; set; } = false;

        public class DeadPlayer
        {
            public uint PlayerId;
            public bool DidPlayOof = false;
            public float Distance = 0;
        }
        public List<DeadPlayer> DeadPlayers { get; set; } = new List<DeadPlayer>();

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
            LoadSoundFile();

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
            Task.Run(() => OofAudioPolling(CancelToken.Token));

        }
        public void LoadSoundFile()
        {
            if (Configuration.DefaultSoundImportPath.Length == 0)
            {
                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.wav");
                this.soundFile = path;

                return;
            }
            this.soundFile = Configuration.DefaultSoundImportPath;
        }
        private void OnCommand(string command, string args)
        {
            if (command == oofCommand) PlaySound();
            if (command == oofSettings) this.PluginUi.SettingsVisible = true;
            if (command == oofVideo) OpenVideo();

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
                PluginLog.Error("failed to check for oof condition:", e.Message);
            }
        }
      
        /// <summary>
        /// check if player has died during alliance, party, and self.
        /// this may be the worst if statement chain i have made
        /// </summary>
        private void CheckDeath()
        {
            if (PartyList != null && PartyList.Any())
            {
                if(Configuration.OofOthersInAlliance && PartyList.Length == 8 && PartyList.GetAllianceMemberAddress(0) != IntPtr.Zero) // the worst "is alliance" check
                {
                    try
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            var allianceMemberAddress = PartyList.GetAllianceMemberAddress(i);
                            if (allianceMemberAddress == IntPtr.Zero) throw new NullReferenceException("allience member address is null");
                              
                            var allianceMember = PartyList.CreateAllianceMemberReference(allianceMemberAddress) ?? throw new NullReferenceException("allience reference is null");
                            AddRemoveDeadPlayer(allianceMember);
                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.LogError("failed alliance check", e.Message);
                    }
                }
                if (Configuration.OofOthersInParty)
                {
                    foreach (var member in PartyList)
                    {
                        AddRemoveDeadPlayer(member,member.Territory.Id == ClientState!.TerritoryType);
                    }
                }
               
            } else
            {
                AddRemoveDeadPlayer(ClientState!.LocalPlayer! );
            }
          
        }

        /// <summary>
        /// Handle Player death, and add distance if true
        /// </summary>
        /// <param name="character">character objectId</param>
        /// <param name="currentHp">character's current hp</param>
        /// <param name="condition">extra condition</param>
        private void AddRemoveDeadPlayer(PlayerCharacter character, bool condition = true)
        {

            if (character == null) return;
            if (character.CurrentHp == 0 && !DeadPlayers.Any(x => x.PlayerId == character.ObjectId) && condition)
            {
                DeadPlayers.Add(new DeadPlayer { PlayerId = character.ObjectId });
            }
            else if (character.CurrentHp != 0 && DeadPlayers.Any(x => x.PlayerId == character.ObjectId))
            {
                DeadPlayers.RemoveAll(x => x.PlayerId == character.ObjectId);
            }
        }
        private void AddRemoveDeadPlayer(PartyMember character, bool condition = true)
        {
            if (character == null) return;
            float distance = 0;
            if (true)
            {
                var localPlayerPos = ClientState!.LocalPlayer!.Position;
                distance = Vector3.Distance(localPlayerPos, character.Position);
            }

            if (character.CurrentHP == 0 && !DeadPlayers.Any(x => x.PlayerId == character.ObjectId) && condition)
            {
                DeadPlayers.Add(new DeadPlayer { PlayerId = character.ObjectId,Distance = distance });
            }
            else if (character.CurrentHP != 0 && DeadPlayers.Any(x => x.PlayerId == character.ObjectId))
            {
                DeadPlayers.RemoveAll(x => x.PlayerId == character.ObjectId);
            }
        }
        /// <summary>
        /// check if player has taken fall damage (brute force way)
        /// </summary>
        private void CheckFallen()
        {
            // dont run btwn moving areas
            if ( Condition[ConditionFlag.InCombat] || Condition[ConditionFlag.BetweenAreas]) return;
            if (!Configuration.OofWhileMounted && (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2])) return;

            var isJumping = Condition[ConditionFlag.Jumping];
            var pos = ClientState!.LocalPlayer!.Position.Y;
            var velocity = prevPos - pos;

            if (isJumping && !wasJumping)
            {
                if (prevVel < 0.17) distJump = pos; //started falling
            }
            else if (wasJumping && !isJumping)  // stopped falling
            {
                if (distJump - pos > 9.60) Task.Run(() => PlaySound()); // fell enough to take damage // i guessed and checked this distance value btw
            }

            // set position for next timestep
            prevPos = pos;
            prevVel = velocity;
            wasJumping = isJumping;
        }

        /// <summary>
        /// Play sound but without referencing windows.forms.
        /// much of the code from: https://github.com/kalilistic/Tippy/blob/5c18d6b21461b0bbe4583a86787ef4a3565e5ce6/src/Tippy/Tippy/Logic/TippyController.cs#L11
        /// </summary>
        /// <param name="volume">optional volume param</param>
        public void PlaySound(float volume = 1)
        {
                ///if (this.isSoundPlaying) this.soundOut!.Stop();
                var soundDevice = DirectSoundOut.Devices.FirstOrDefault();
                if (soundDevice == null)
                {
                    PluginLog.Error("no sound device lmao");
                    return;
                }

                this.isSoundPlaying = true;
                //this.reader = new WaveFileReader();
                WaveStream reader;
                try
                {
                    reader = new MediaFoundationReader(this.soundFile);
                }
                catch (Exception ex)
                {
                    this.isSoundPlaying = false;
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
                    //shoutout anna clemens for the winforms fix
                    using var output = new DirectSoundOut();

                    try
                    {
                        output.Init(audioStream);
                        output.Play();
                        output.PlaybackStopped += OnPlaybackStopped;
                        void OnPlaybackStopped(object? sender, StoppedEventArgs e)
                        {
                            output.PlaybackStopped -= OnPlaybackStopped;
                            this.isSoundPlaying = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.isSoundPlaying = false;
                        PluginLog.Error("Failed play sound", ex);
                        return;
                    }
                }


           
        }

        /// <summary>
        /// run after sound has played.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
       

        /// <summary>
        /// open the hbomberguy video on oof
        /// </summary>
        public void OpenVideo()
        {
            Util.OpenLink("https://www.youtube.com/watch?v=0twDETh6QaI");
        }

        /// <summary>
        /// check deadPlayers every once in a while. prevents multiple oof from playing too fast
        /// </summary>
        /// <param name="token"> cancellation token</param>
        private void OofAudioPolling(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) break;
                Task.Delay(200).Wait();
                if (!DeadPlayers.Any()) continue;
           
                foreach (var player in DeadPlayers)
                {
                    if (!player.DidPlayOof)
                    {



                        PlaySound();
                        player.DidPlayOof = true;
                        break;
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

               
            }
            catch (Exception e)
            {
                PluginLog.LogError("Failed to dispose oofplugin controller", e);
            }


        }


    }
}

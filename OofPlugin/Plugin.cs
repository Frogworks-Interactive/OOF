using Dalamud.Game;
using Dalamud.Game.ClientState;
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
//shoutout anna clemens

namespace OofPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "OOF";

        private const string oofCommand = "/oof";
        private const string oofSettings = "/oofsettings";

        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static Condition Condition { get; private set; } = null!;

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
        public bool isDead { get; set; } = false;

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
            this.soundFile = File.ReadAllBytes(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.wav"));

            this.CommandManager.AddHandler(oofCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "play oof sound"
            });
            this.CommandManager.AddHandler(oofSettings, new CommandInfo(OnCommand)
            {
                HelpMessage = "change oof settings"
            });
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += this.FrameworkOnUpdate;
        }

        private void OnCommand(string command, string args)
        {
            if (command == oofCommand) PlaySound();
            if (command == oofSettings) this.PluginUi.SettingsVisible = true;
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
            if (ClientState!.LocalPlayer!.CurrentHp == 0 && !isDead)
            {
                PlaySound();
                isDead = true;
            }
            else if (ClientState!.LocalPlayer!.CurrentHp != 0 && isDead)
            {
                isDead = false;
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
                if (distJump - pos > 9.50) PlaySound(); // fell enough to take damage
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
        /// <param name="num">sound to play.</param>
        public void PlaySound()
        {
            try
            {
                if (this.isSoundPlaying) return;

                var soundDevice = DirectSoundOut.Devices.FirstOrDefault();
                if (soundDevice == null)
                {
                    PluginLog.Error("no sound device lmao");
                    return;
                }

                this.isSoundPlaying = true;
                this.reader = new WaveFileReader(new MemoryStream(this.soundFile));

                var audioStream = new WaveChannel32(this.reader);
                audioStream.Volume = Configuration.Volume;
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
        /// dispose
        /// </summary>
        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(oofCommand);
            this.CommandManager.RemoveHandler(oofSettings);

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

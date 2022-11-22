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
using System.Threading;

namespace SamplePlugin
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
        //    public PlayerCharacter LocalPlayer { get; set; } = null!;

        // sound
        public bool isSoundPlaying { get; set; } = false;
        private Mp3FileReader? reader;
        private readonly WaveOut waveOut = new();
        private byte[] soundFile { get; set; }

        //check
        public float prevPos { get; set; } = 0;
        private float prevVel { get; set; } = 0;
        //    public float distFallen { get; set; } = 0;
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

            // you might normally want to embed resources and load them from the manifest stream // what if i said no
            this.PluginUi = new PluginUI(this.Configuration, this);
            var soundfile = File.ReadAllBytes(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "oof.mp3"));
            this.soundFile = soundfile;

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
        private void CheckFallen()
        {   
            // dont run if mounted

            if (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2] || Condition[ConditionFlag.Mounted2] || Condition[ConditionFlag.InCombat]) return;
            var isJumping = Condition[ConditionFlag.Jumping];

            var pos = ClientState!.LocalPlayer!.Position.Y;
            var velocity = prevPos - pos;
            if (isJumping && !wasJumping)
            {
                //started falling
                if (prevVel < 0.17) distJump = pos;
            }

            // stopped falling
            else if (wasJumping && !isJumping)
            {
                // fell enough to take damage
                if (distJump - pos > 9.50) PlaySound();

            }

            // set position for next timestep
            prevPos = pos;
            prevVel = velocity;
            wasJumping = isJumping == true;
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
        /// Play sound.
        /// </summary>
        /// <param name="num">sound to play.</param>
        public void PlaySound()
        {
            try
            {
                if (this.isSoundPlaying) return;
                this.isSoundPlaying = true;
                this.reader = new Mp3FileReader(new MemoryStream(this.soundFile));
                this.waveOut.Init(this.reader);
                this.waveOut.Volume = Configuration.Volume;
                this.waveOut.Play();
                this.waveOut.PlaybackStopped += this.WaveOutOnPlaybackStopped;
            }
            catch (Exception e)
            {
                this.isSoundPlaying = false;
                PluginLog.Error("failed to play oof:", e);
            }
        }

        private void WaveOutOnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            this.waveOut.PlaybackStopped -= this.WaveOutOnPlaybackStopped;
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

                this.isSoundPlaying = true;
                this.waveOut.Dispose();
            }
            catch (Exception ex)
            {
                PluginLog.LogError("Failed to dispose tippy controller", ex);
            }


        }


    }
}

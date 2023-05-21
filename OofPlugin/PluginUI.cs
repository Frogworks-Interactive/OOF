using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface;
using System.Text.RegularExpressions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;

namespace OofPlugin
{
    partial class PluginUI : IDisposable
    {
        private Configuration configuration;
        private Plugin plugin;
        private FileDialogManager manager { get; }

        private bool settingsVisible = false;
       
        public bool SettingsVisible
        {
            get { return settingsVisible; }
            set { settingsVisible = value; }
        }

        public PluginUI(Configuration configuration, Plugin plugin)
        {
            this.configuration = configuration;
            this.plugin = plugin;
            manager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(380, 420), ImGuiCond.Once);
            if (ImGui.Begin("oof options", ref settingsVisible,
                 ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var oofVolume = configuration.Volume;
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Volume");


                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * ImGuiHelpers.GlobalScale);
                
                if (ImGui.SliderFloat("###volume", ref oofVolume, 0.0f, 1.0f))
                {
                    configuration.Volume = oofVolume;
                    configuration.Save();
                }
                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Play Oof On");
                ImGuiComponents.HelpMarker(
                  "turn on/off various conditions to play the sound on");
                ImGui.Columns(2);

                void CheckboxHelper(string title, bool config)
                {
                    var oofOnDeath = config;

                    if (ImGui.Checkbox(title, ref oofOnDeath))
                    {
                        config = oofOnDeath;
                        configuration.Save();
                    }

                }
                CheckboxHelper("Death###play-oof-death", configuration.OofOnDeath);
                CheckboxHelper("Fall damage###play-oof-fall", configuration.OofOnFall);
                CheckboxHelper("During combat###play-oof-combat", configuration.OofInBattle);
                CheckboxHelper("While mounted###play-oof-mounted", configuration.OofWhileMounted);
                ImGui.NextColumn();
                CheckboxHelper("Party member's death###play-oof-party", configuration.OofOthersInParty);
                CheckboxHelper("Alliance member's death###play-oof-alliance", configuration.OofOthersInAlliance);


                //var oofOnFall = configuration.OofOnFall;

                //if (ImGui.Checkbox("Fall damage###play-oof-fall", ref oofOnFall))
                //{
                //    configuration.OofOnFall = oofOnFall;
                //    configuration.Save();
                //}
                //var oofInBattle = configuration.OofInBattle;

                //if (ImGui.Checkbox("During combat###play-oof-combat", ref oofInBattle))
                //{
                //    configuration.OofInBattle = oofInBattle;
                //    configuration.Save();
                //}
                //var oofWhileMounted = configuration.OofWhileMounted;

                //if (ImGui.Checkbox("While mounted###play-oof-mounted", ref oofWhileMounted))
                //{
                //    configuration.OofWhileMounted = oofWhileMounted;
                //    configuration.Save();
                //}
                //ImGui.NextColumn();
                //var oofOthersInParty = configuration.OofOthersInParty;

                //if (ImGui.Checkbox("Party member's death###play-oof-party", ref oofOthersInParty))
                //{
                //    configuration.OofOthersInParty = oofOthersInParty;
                //    configuration.Save();
                //}
                //var oofOthersInAlliance = configuration.OofOthersInAlliance;

                //if (ImGui.Checkbox("Alliance member's death###play-oof-alliance", ref oofOthersInAlliance))
                //{
                //    configuration.OofOthersInAlliance = oofOthersInAlliance;
                //    configuration.Save();
                //}
                ImGui.Columns(1);

                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Loaded SoundFile");
                ImGuiComponents.HelpMarker(
                   "Use a custom audio file from computer. ");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.PlayCircle)) plugin.PlaySound(plugin.CancelToken.Token);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Play");

                ImGui.SameLine();


                if (configuration.DefaultSoundImportPath.Length > 0)
                {
                    var formatString = getFileName().Match(configuration.DefaultSoundImportPath);
                    if (formatString.Success)
                    {
                        ImGui.TextUnformatted(formatString.Value);
                    }
                }
                else
                {
                    ImGui.TextUnformatted("Original Oof.wav");
                }

                if (ImGui.Button("Browse sound file"))
                {
                    void UpdatePath(bool success, string path)
                    {
                        if (!success || path.Length == 0)
                        {
                        return;
                        }
                        configuration.DefaultSoundImportPath = path;
                        configuration.Save();
                        plugin.LoadSoundFile();
                }

                    manager.OpenFileDialog("Open Audio File...", "Audio{.wav,.mp3,.aac,.wma}", UpdatePath);
                    }
                    ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    configuration.DefaultSoundImportPath = string.Empty;
                    configuration.Save();
                    plugin.LoadSoundFile();

                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset");
                ImGui.Separator();
                ImGui.TextWrapped("Learn about the history behind the Roblox Oof with Hbomberguy's Documentary:");

                if (ImGui.Button("Watch on Youtube")) Plugin.OpenVideo();
                var desc = "Hot Tip: Macro the /oofvideo command to add a shortcut for the video for easy and streamlined access.";
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, desc);
                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Original Oof sound by Joey Kuras");


                manager.Draw();

            }

            ImGui.End();
        }
        // Set up the file selector with the right flags and custom side bar items.
        public static FileDialogManager SetupFileManager()
        {
            var fileManager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };

            // Remove Videos and Music.
            fileManager.CustomSideBarItems.Add(("Videos", string.Empty, 0, -1));
            fileManager.CustomSideBarItems.Add(("Music", string.Empty, 0, -1));

            return fileManager;
        }

        /// <summary>
        /// get file name from file path string
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("[^\\\\]+$")]
        private static partial Regex getFileName();
    }
}

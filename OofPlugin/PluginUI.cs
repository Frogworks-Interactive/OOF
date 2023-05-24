using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using System;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
namespace OofPlugin
{
    partial class PluginUI : IDisposable
    {
        private Configuration configuration;

        private Plugin plugin;
        private readonly TextureWrap creditsTexture;
        private FileDialogManager manager { get; }
        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return settingsVisible; }
            set { settingsVisible = value; }
        }
        public PluginUI(Configuration configuration, Plugin plugin, DalamudPluginInterface pluginInterface)
        {
            this.configuration = configuration;
            this.plugin = plugin;
            manager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };
            var imagePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "credits.png");

            this.creditsTexture = pluginInterface.UiBuilder.LoadImage(imagePath)!;
        }

        public void Draw()
        {
            DrawSettingsWindow();
        }
        public void DrawSettingsWindow()
        {
            if (!SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(380, 460), ImGuiCond.Appearing);
            if (ImGui.Begin("oof options", ref settingsVisible,
                 ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var oofVolume = configuration.Volume;
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Volume");


                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                if (ImGui.SliderFloat("###volume", ref oofVolume, 0.0f, 1.0f))
                {
                    configuration.Volume = oofVolume;
                    configuration.Save();
                }
                ImGui.Separator();
                //ImGuiComponents.HelpMarker(
                //  "turn on/off various conditions to trigger sound");
                var oofOnFall = configuration.OofOnFall;
                SectionStart();
                SectionHeader("OofOnFall", ref oofOnFall, () => { configuration.OofOnFall = oofOnFall; });

                var oofWhileMounted = configuration.OofWhileMounted;
                if (ImGui.Checkbox("While mounted###play-oof-mounted", ref oofWhileMounted))
                {
                    configuration.OofWhileMounted = oofWhileMounted;
                    configuration.Save();
                }
                SectionEnd();

                ImGui.Columns(2);

                var oofOnDeath = configuration.OofOnDeath;

                if (ImGui.Checkbox("Death (self)###play-oof-death", ref oofOnDeath))
                {
                    configuration.OofOnDeath = oofOnDeath;
                    configuration.Save();
                }

                var oofInBattle = configuration.OofInBattle;


                if (ImGui.Checkbox("During Combat###play-oof-combat", ref oofInBattle))
                {
                    configuration.OofInBattle = oofInBattle;
                    configuration.Save();
                }

                ImGui.NextColumn();
                var oofOthersInParty = configuration.OofOthersInParty;

                if (ImGui.Checkbox("Party member's death###play-oof-party", ref oofOthersInParty))
                {
                    configuration.OofOthersInParty = oofOthersInParty;
                    configuration.Save();
                }
                var oofOthersInAlliance = configuration.OofOthersInAlliance;

                if (ImGuiComponents.ToggleButton("Alliance member's death###play-oof-alliance", ref oofOthersInAlliance))
                {
                    configuration.OofOthersInAlliance = oofOthersInAlliance;
                    configuration.Save();
                }
                ImGui.Columns(1);

                ImGui.Separator();
                LoadAudioUI();
                ImGui.Separator();
                ImGui.TextWrapped("Learn about the history behind the Roblox Oof with Hbomberguy's Documentary:");

                if (ImGui.Button("Watch on Youtube")) Plugin.OpenVideo();
                var desc = "Hot Tip: Macro the /oofvideo command to add a shortcut for the video for easy and streamlined access.";
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, desc);
                ImGui.Separator();

                //logo
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Original Oof sound by Joey Kuras");
                var size = new Vector2(this.creditsTexture.Width * (float)0.60, this.creditsTexture.Height * (float)0.60);
                var diff = new Vector2(11, 11);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 13);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.SetCursorPos(ImGui.GetWindowSize() - size - diff);

                if (ImGui.ImageButton(this.creditsTexture.ImGuiHandle, size)) Util.OpenLink("https://github.com/Frogworks-Interactive");
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    ImGui.BeginTooltip();
                    ImGui.Text("Visit Github");

                    ImGui.EndTooltip();
                }
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
        public void Dispose()
        {
        }
    }

}

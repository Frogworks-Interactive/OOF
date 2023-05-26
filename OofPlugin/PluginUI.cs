using Dalamud.Interface;
using Dalamud.Interface.Colors;
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

            ImGui.SetNextWindowSize(new Vector2(380, 470), ImGuiCond.Appearing);
            if (ImGui.Begin("oof options", ref settingsVisible,
                 ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // volume with icons
                var oofVolume = configuration.Volume;
                var headingColor = ImGuiColors.DalamudGrey;
                ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Volume");
                ImGui.AlignTextToFramePadding();
                IconTextColor(FontAwesomeIcon.VolumeMute.ToIconString(), headingColor);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFontSize() * 1.6f);
                if (ImGui.SliderFloat("###volume", ref oofVolume, 0.0f, 1.0f))
                {
                    configuration.Volume = oofVolume;
                    configuration.Save();
                }

                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                IconTextColor(FontAwesomeIcon.VolumeUp.ToIconString(), headingColor);
                ImGui.Spacing();

                LoadAudioUI();
                ImGui.Spacing();

                ImGui.Separator();
                ImGui.Spacing();

                //ImGuiComponents.HelpMarker(
                //  "turn on/off various conditions to trigger sound");
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Play sound on");

                // when self falls options
                var oofOnFall = configuration.OofOnFall;
                SectionStart();
                SectionHeader("Fall Damage", ref oofOnFall, () => { configuration.OofOnFall = oofOnFall; });
                if (!oofOnFall) ImGui.BeginDisabled();
                ImGui.Columns(2);

                var oofOnFallMounted = configuration.OofOnFallMounted;
                if (ImGui.Checkbox("While mounted###fall:mounted", ref oofOnFallMounted))
                {
                    configuration.OofOnFallMounted = oofOnFallMounted;
                    configuration.Save();
                }
                ImGui.NextColumn();

                var oofOnFallBattle = configuration.OofOnFallBattle;
                if (ImGui.Checkbox("During combat###fall:combat", ref oofOnFallBattle))
                {
                    configuration.OofOnFallBattle = oofOnFallBattle;
                    configuration.Save();
                }
                ImGui.Columns(1);
                if (!oofOnFall) ImGui.EndDisabled();

                SectionEnd();

                // when people die options
                SectionStart();
                var oofOnDeath = configuration.OofOnDeath;

                SectionHeader("Death", ref oofOnDeath, () => { configuration.OofOnDeath = oofOnDeath; });
                if (!oofOnDeath) ImGui.BeginDisabled();

                ImGui.Columns(2);
                var oofOnDeathSelf = configuration.OofOnDeathSelf;

                if (ImGui.Checkbox("Self dies###death:self", ref oofOnDeathSelf))
                {
                    configuration.OofOnDeathSelf = oofOnDeathSelf;
                    configuration.Save();
                }

                var oofInBattle = configuration.OofOnDeathBattle;

                if (ImGui.Checkbox("During combat###death:combat", ref oofInBattle))
                {
                    configuration.OofOnDeathBattle = oofInBattle;
                    configuration.Save();
                }

                ImGui.NextColumn();
                var oofOthersInParty = configuration.OofOnDeathParty;

                if (ImGui.Checkbox("Party member dies###death:party", ref oofOthersInParty))
                {
                    configuration.OofOnDeathParty = oofOthersInParty;
                    configuration.Save();
                }
                var oofOnDeathAlliance = configuration.OofOnDeathAlliance;

                if (ImGui.Checkbox("Alliance member dies###death:alliance", ref oofOnDeathAlliance))
                {
                    configuration.OofOnDeathAlliance = oofOnDeathAlliance;
                    configuration.Save();
                }
                ImGui.Columns(1);
                if (!oofOnDeath) ImGui.EndDisabled();

                SectionEnd();
                ImGui.Spacing();
                ImGui.Spacing();

                ImGui.Separator();
                ImGui.Spacing();

                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudWhite2, "Learn about the history behind the Roblox Oof with Hbomberguy's Documentary:");

                if (ImGui.Button("Watch on Youtube")) Plugin.OpenVideo();
                var desc = "Hot Tip: You can Macro the /oofvideo command to\n for easy and streamlined access to this video.";

                if (ImGui.IsItemHovered()) ImGui.SetTooltip(desc);
                ImGui.Spacing();
                ImGui.Spacing();

                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Original Oof sound by Joey Kuras");

                //logo
                var size = new Vector2(this.creditsTexture.Width * (float)0.60, this.creditsTexture.Height * (float)0.60);
                var diff = new Vector2(0, 0);
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

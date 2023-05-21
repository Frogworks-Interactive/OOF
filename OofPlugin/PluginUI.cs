using ImGuiNET;
using Lumina.Data;
using NAudio.Wave;
using System;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using System.Collections.Generic;
using ImGuiScene;
using Lumina.Models.Materials;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Dalamud.Logging;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;

namespace OofPlugin
{
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private Plugin plugin;
        private FileDialogManager manager { get; }

        private bool settingsVisible = false;
       
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public PluginUI(Configuration configuration, Plugin plugin)
        {
            this.configuration = configuration;
            this.plugin = plugin;
            this.manager = new FileDialogManager
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
            if (ImGui.Begin("oof options", ref this.settingsVisible,
                 ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var oofVolume = this.configuration.Volume;
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Volume");


                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * ImGuiHelpers.GlobalScale);

                if (ImGui.SliderFloat("###volume", ref oofVolume, 0.0f, 1.0f))
                {
                    this.configuration.Volume = oofVolume;
                    this.configuration.Save();
                }
                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Play Oof On");
                ImGuiComponents.HelpMarker(
                  "turn on/off various conditions to play the sound on");
                ImGui.Columns(2);

                var oofOnDeath = this.configuration.OofOnDeath;

                if (ImGui.Checkbox("Death###play-oof-death", ref oofOnDeath))
                {
                    this.configuration.OofOnDeath = oofOnDeath;
                    this.configuration.Save();
                }
              
                var oofOnFall = this.configuration.OofOnFall;

                if (ImGui.Checkbox("Fall damage###play-oof-fall", ref oofOnFall))
                {
                    this.configuration.OofOnFall = oofOnFall;
                    this.configuration.Save();
                }
                var oofInBattle = this.configuration.OofInBattle;

                if (ImGui.Checkbox("During combat###play-oof-combat", ref oofInBattle))
                {
                    this.configuration.OofInBattle = oofInBattle;
                    this.configuration.Save();
                }
                var oofWhileMounted = this.configuration.OofWhileMounted;

                if (ImGui.Checkbox("While mounted###play-oof-mounted", ref oofWhileMounted))
                {
                    this.configuration.OofWhileMounted = oofWhileMounted;
                    this.configuration.Save();
                }
                ImGui.NextColumn();
                var oofOthersInParty = this.configuration.OofOthersInParty;

                if (ImGui.Checkbox("Party member's death###play-oof-party", ref oofOthersInParty))
                {
                    this.configuration.OofOthersInParty = oofOthersInParty;
                    this.configuration.Save();
                }
                var oofOthersInAlliance = this.configuration.OofOthersInAlliance;

                if (ImGui.Checkbox("Alliance member's death###play-oof-alliance", ref oofOthersInAlliance))
                {
                    this.configuration.OofOthersInAlliance = oofOthersInAlliance;
                    this.configuration.Save();
                }
                ImGui.Columns(1);

                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Loaded SoundFile");
                ImGuiComponents.HelpMarker(
                   "Load a custom audio file from computer. Must be a WAV file (idk how to do mp3)");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.PlayCircle)) plugin.PlaySound();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Play");

                ImGui.SameLine();


                if (this.configuration.DefaultSoundImportPath.Length > 0)
                {
                    var formatString = Regex.Match(this.configuration.DefaultSoundImportPath, @"[^\\]+$");
                    if (formatString.Success)
                    {
                        ImGui.TextUnformatted(formatString.Value);
                    }
                }
                else
                {
                    ImGui.TextUnformatted("Original Oof.wav");
                }

                if (ImGui.Button("Browse .WAV sound file"))
                {
                    void UpdatePath(bool success, string path)
                    {
                        if (!success || path.Length == 0)
                        {
                        return;
                        }
                        this.configuration.DefaultSoundImportPath = path;
                        this.configuration.Save();
                        plugin.LoadSoundFile();
                }

                    manager.OpenFileDialog("Open Audio File...", "Audio{.wav}", UpdatePath);
                    }
                    ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    this.configuration.DefaultSoundImportPath = string.Empty;
                    this.configuration.Save();
                    plugin.LoadSoundFile();

                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset");
                ImGui.Separator();
                ImGui.TextWrapped("Learn about the history behind the Roblox Oof with Hbomberguy's Documentary:");

                if (ImGui.Button("Watch on Youtube")) plugin.OpenVideo();
                var desc = "Tip: Macro the /oofvideo command to add a shortcut for the video for easy and streamlined access.";
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, desc);
                ImGui.Separator();
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Original Oof sound by Joey Kuras");


                this.manager.Draw();

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
    }
}

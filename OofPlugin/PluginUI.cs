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

        public void CheckMark()
        {

        }
        public void DrawSettingsWindow()
        {
            if (!SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(400, 340), ImGuiCond.Always);
            if (ImGui.Begin("oof options", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var oofVolume = this.configuration.Volume;

                if (ImGui.SliderFloat("Volume", ref oofVolume, 0.0f, 1.0f))
                {
                    this.configuration.Volume = oofVolume;
                    this.configuration.Save();
                }
                ImGui.Columns(2);

                var oofOnDeath = this.configuration.OofOnDeath;

                if (ImGui.Checkbox("Oof on death###play-oof-death", ref oofOnDeath))
                {
                    this.configuration.OofOnDeath = oofOnDeath;
                    this.configuration.Save();
                }
                ImGui.NextColumn();
                var oofOnFall = this.configuration.OofOnFall;

                if (ImGui.Checkbox("Oof on fall damage###play-oof-fall", ref oofOnFall))
                {
                    this.configuration.OofOnFall = oofOnFall;
                    this.configuration.Save();
                }
                ImGui.NextColumn();
                var oofInBattle = this.configuration.OofInBattle;

                if (ImGui.Checkbox("Oof during combat###play-oof-combat", ref oofInBattle))
                {
                    this.configuration.OofInBattle = oofInBattle;
                    this.configuration.Save();
                }
                ImGui.NextColumn();
                var oofOthersInParty = this.configuration.OofOthersInParty;

                if (ImGui.Checkbox("Oof on party member's death###play-oof-party", ref oofOthersInParty))
                {
                    this.configuration.OofOthersInParty = oofOthersInParty;
                    this.configuration.Save();
                }
                ImGui.NextColumn();
                var oofOthersInAlliance = this.configuration.OofOthersInAlliance;

                if (ImGui.Checkbox("Oof on alliance member's death###play-oof-alliance", ref oofOthersInAlliance))
                {
                    this.configuration.OofOthersInAlliance = oofOthersInAlliance;
                    this.configuration.Save();
                }
                ImGui.Columns(1);

                ImGui.Separator();
                ImGui.TextUnformatted("Loaded SoundFile:");
                if (ImGui.Button("Play")) plugin.PlaySound();
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
                        plugin.loadSoundFile();
                }

                    manager.OpenFileDialog("Open Image...", "Audio{.wav}", UpdatePath);
                    }
                    ImGui.SameLine();
                if (ImGui.Button("reset"))
                {
                    this.configuration.DefaultSoundImportPath = string.Empty;
                    this.configuration.Save();
                    plugin.loadSoundFile();

                }
                ImGui.Separator();
                ImGui.TextWrapped("Learn about the history behind the Roblox Oof with Hbomberguy's Documentary.");


                if (ImGui.Button("Watch on Youtube")) plugin.openVideo();

                ImGui.TextWrapped("Original Oof sound by Joey Kuras");

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

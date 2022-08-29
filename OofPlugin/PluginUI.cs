using ImGuiNET;
using System;
using System.Numerics;

namespace SamplePlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private Plugin plugin;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

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
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 200), ImGuiCond.Always);
            if (ImGui.Begin("oof settings", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {

                var oofOnDeath = this.configuration.OofOnDeath;

                if (ImGui.Checkbox("Play oof on death###play-oof-death", ref oofOnDeath))
                {
                    this.configuration.OofOnDeath = oofOnDeath;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

                var oofOnFall = this.configuration.OofOnFall;

                if (ImGui.Checkbox("Play oof on fall damage###play-oof-fall", ref oofOnFall))
                {
                    this.configuration.OofOnFall = oofOnFall;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                var oofVolume = this.configuration.Volume;

                if (ImGui.SliderFloat("volume", ref oofVolume, 0.0f, 1.0f))
                {
                    this.configuration.Volume = oofVolume;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }
    }
}

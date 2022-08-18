using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;

namespace SamplePlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private Plugin plugin;

        private ImGuiScene.TextureWrap goatImage;

        private float prevPos { get; set; } = 0;
        private float prevVel { get; set; } = 0;
        private float distFallen { get; set; } = 0;
        private float distJump { get; set; } = 0;

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

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, Plugin plugin)
        {
            this.configuration = configuration;
            this.plugin = plugin;
  

        }

        public void Dispose()
        {
            this.goatImage.Dispose();
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

         //   DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            //ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            //ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            //if (ImGui.Begin("My Amazing Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            //{

            //    if (ImGui.Button("Show Settings"))
            //    {
            //        SettingsVisible = true;
            //    }

            //    ImGui.Text("clientstate");


            //    ImGui.Spacing();

            //    ImGui.Text("Have a goat:");
            //    ImGui.Indent(55);
            //    ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
            //    ImGui.Unindent(55);
            //}
            //ImGui.End();
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

                var oofOnDeath = this.configuration.OofOnFall;

                if (ImGui.Checkbox("Play oof on death", ref oofOnDeath))
                {
                    this.configuration.OofOnDeath = oofOnDeath;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

                var oofOnFall = this.configuration.OofOnFall;

                if (ImGui.Checkbox("Play oof on fall damage", ref oofOnFall))
                {
                    this.configuration.OofOnFall = oofOnFall;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }
    }
}

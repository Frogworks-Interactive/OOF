using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ImGuiNET;
using System;
using System.Numerics;

namespace OofPlugin
{
    public partial class PluginUI
    {

        private class XBounds
        {
            public float Start;
            public float End;

        }
        /// <summary>
        /// calculate x bounds of an element inside window
        /// </summary>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static XBounds calculateSectionBoundsX(float padding)
        {
            float windowStart = ImGui.GetWindowPos().X;
            var start = windowStart + ImGui.GetWindowContentRegionMin().X + padding;
            var end = windowStart + ImGui.GetWindowWidth() - ImGui.GetWindowContentRegionMin().X - padding;
            return new XBounds { Start = start, End = end };
        }
        /// <summary>
        /// create section with a filled background
        /// 
        /// https://github.com/ocornut/imgui/issues/1496#issuecomment-1200143122
        /// </summary>
        public void SectionStart()
        {
            ImGui.GetWindowDrawList().ChannelsSplit(2);
            // Draw content above the rectangle
            ImGui.GetWindowDrawList().ChannelsSetCurrent(1);

            var padding = ImGui.GetStyle().WindowPadding;
            var boundsX = calculateSectionBoundsX(padding.X);
            var rectMin = ImGui.GetItemRectMin();
            var rectMax = ImGui.GetItemRectMax();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding.Y);
            ImGui.BeginGroup();
            if (padding.Y > 0)
            {
                ImGui.Indent(padding.Y);
            }

        }
        /// <summary>
        /// end section with filled bg
        /// </summary>
        /// <param name="color"></param>
        public void SectionEnd(ImGuiCol color = ImGuiCol.MenuBarBg)
        {
            var padding = ImGui.GetStyle().WindowPadding;

            // ImGui.PopClipRect();
            if (padding.X > 0)
            {
                ImGui.Unindent(padding.X);
            }
            ImGui.EndGroup();
            // Essentially, the content is drawn with padding
            // while the rectangle is drawn without padding
            var boundsX = calculateSectionBoundsX(0.0f);

            // GetItemRectMin is going to include the padding
            // as well; so, remove it

            var panelMin = new Vector2(boundsX.Start, ImGui.GetItemRectMin().Y - padding.Y);
            var panelMax = new Vector2(boundsX.End, ImGui.GetItemRectMax().Y + padding.Y);

            // Draw rectangle below
            ImGui.GetWindowDrawList().ChannelsSetCurrent(0);
            ImGui.GetWindowDrawList().AddRectFilled(
                panelMin, panelMax,
                ImGui.GetColorU32(color),
                ImGui.GetStyle().FrameRounding);
            //ImGui.GetWindowDrawList().ChannelsMerge();
            ImGui.GetWindowDrawList().AddRect(
                panelMin, panelMax,
                ImGui.GetColorU32(ImGuiCol.TableBorderLight),
                ImGui.GetStyle().FrameRounding, ImDrawFlags.None, 1.0f);
            ImGui.GetWindowDrawList().ChannelsMerge();
            // Since rectangle is bigger than the box, move the cursor;
            // so, it starts outside the box
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding.Y);

            // Then, add default spacing
            ImGui.Spacing();
        }
        public void SectionHeader(string title, ref bool toggle, Action action1)
        {
            if (ImGuiComponents.ToggleButton($"{title}###${title}", ref toggle))
            {
                action1();
                configuration.Save();
            }
            ImGui.SameLine();
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, title);
            var text = toggle ? "Enabled" : "Disabled";
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SameLine(ImGui.GetWindowWidth() - textSize.X - ImGui.GetFontSize() - 20);
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, $"{text} {FontAwesomeIcon.CheckCircle.ToIconString()}");
        }
        private void LoadAudioUI()
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Loaded SoundFile");
            ImGuiComponents.HelpMarker(
               "Use a custom audio file from computer. ");
            if (ImGuiComponents.IconButton(FontAwesomeIcon.PlayCircle)) plugin.PlaySound(plugin.CancelToken.Token);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Play");

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
        }


    }
}

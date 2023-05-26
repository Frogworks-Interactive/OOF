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
            //var boundsX = calculateSectionBoundsX(padding.X);
            //var rectMin = ImGui.GetItemRectMin();
            //var rectMax = ImGui.GetItemRectMax();
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


        }
        public void SectionHeader(string title, ref bool toggle, Action action1)
        {
            var padding = ImGui.GetStyle().WindowPadding;
            var text = toggle ? "Enabled" : "Disabled";
            var color = toggle ? ImGuiColors.DalamudWhite2 : ImGuiColors.DalamudGrey;

            var textSize = ImGui.CalcTextSize(text);
            ImGui.AlignTextToFramePadding();

            if (ImGuiComponents.ToggleButton($"{title}###${title}", ref toggle))
            {
                action1();
                configuration.Save();
            }
            ImGui.SameLine();
            ImGuiHelpers.SafeTextColoredWrapped(color, title);

            ImGui.SameLine(ImGui.GetWindowWidth() - textSize.X - ImGui.GetFontSize() * 1.7f - padding.X * 2);
            ImGuiHelpers.SafeTextColoredWrapped(color, $"{text}");
            ImGui.SameLine();
            var iconString = toggle ? FontAwesomeIcon.CheckSquare.ToIconString() : FontAwesomeIcon.SquareXmark.ToIconString();
            IconTextColor(iconString, color);
            ImGui.Spacing();
        }

        private void IconTextColor(string text, Vector4 color = new Vector4())
        {
            if (color == Vector4.Zero) color = ImGuiColors.DalamudWhite;
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiHelpers.SafeTextColoredWrapped(color, text);
            ImGui.PopFont();
        }

        private float CalcButtonSize(string text)
        {
            return ImGui.CalcTextSize(text).X + ImGui.GetStyle().FramePadding.X * 2;
        }
        private float CalcButtonSize(float value)
        {
            return value + ImGui.GetStyle().FramePadding.X * 2;
        }
        private void LoadAudioUI()
        {
            var WindowPos = ImGui.GetWindowPos();
            var windowPadding = ImGui.GetStyle().WindowPadding;
            var em = ImGui.GetFontSize();
            var draw = ImGui.GetWindowDrawList();

            ImGui.AlignTextToFramePadding();

            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Loaded SoundFile");
            ImGuiComponents.HelpMarker(
               "You can upload a custom audio file from computer. ");
            ImGui.SameLine(ImGui.GetWindowWidth() - CalcButtonSize(em) - windowPadding.X);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
            {
                configuration.DefaultSoundImportPath = string.Empty;
                configuration.Save();
                plugin.LoadSoundFile();

            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Reset audio file to oof");


            if (ImGuiComponents.IconButton(FontAwesomeIcon.Play)) plugin.PlaySound(plugin.CancelToken.Token);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Play");

            ImGui.SameLine(0, 0);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Stop)) plugin.StopSound();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Stop");

            ImGui.SameLine();
            var soundFileName = "Original Oof.wav";

            if (configuration.DefaultSoundImportPath.Length > 0)
            {
                var formatString = getFileName().Match(configuration.DefaultSoundImportPath);
                if (formatString.Success) soundFileName = formatString.Value;

            }
            customDraggableText(soundFileName);

            var browseText = "Upload Audio";

            ImGui.SameLine(ImGui.GetWindowWidth() - CalcButtonSize(browseText) - windowPadding.X);

            if (ImGui.Button(browseText))
            {
                void UpdatePath(bool success, string path)
                {
                    if (!success || path.Length == 0) return;

                    configuration.DefaultSoundImportPath = path;
                    configuration.Save();
                    plugin.LoadSoundFile();
                }

                manager.OpenFileDialog("Open Audio File...", "Audio{.wav,.mp3,.aac,.wma}", UpdatePath);
            }
            ImGui.Spacing();
        }
        /// <summary>
        /// theres no reason to use this over an input box but it was fun to make
        /// </summary>
        private void customDraggableText(string text)
        {
            var WindowPos = ImGui.GetWindowPos();
            var draw = ImGui.GetWindowDrawList();
            var em = ImGui.GetFontSize();

            var cursorPos = ImGui.GetCursorPos();
            var panelMin = new Vector2(cursorPos.X + WindowPos.X, ImGui.GetItemRectMin().Y);
            var panelMax = new Vector2(WindowPos.X + ImGui.GetContentRegionAvail().X - CalcButtonSize(em) - ImGui.GetStyle().ItemSpacing.X, ImGui.GetItemRectMax().Y);
            var boxSize = panelMax - panelMin;

            var shouldScroll = false;
            if (ImGui.CalcTextSize(text).X > boxSize.X)
            {
                shouldScroll = true;
            }
            ImGui.GetWindowDrawList().PushClipRect(panelMin, panelMax, true);



            ImGui.SetCursorPos(cursorPos);
            ImGui.InvisibleButton("###soundbtn", panelMax - panelMin);
            if (ImGui.IsItemHovered() && shouldScroll) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            var dist = panelMin.X;

            if (ImGui.IsItemActive() && shouldScroll)
            {

                var io = ImGui.GetIO();
                var newDist = +io.MousePos.X - io.MouseClickedPos[0].X;
                if (panelMin.X + newDist > panelMin.X) newDist = 0;
                else if (panelMin.X + newDist + ImGui.CalcTextSize(text).X < panelMax.X) newDist = boxSize.X - ImGui.CalcTextSize(text).X;

                dist = panelMin.X + newDist;
                //draw.AddLine(io.MouseClickedPos[0], io.MousePos, ImGui.GetColorU32(ImGuiCol.Button), 4.0f);

            }
            var framePadding = ImGui.GetStyle().FramePadding;
            draw.AddText(new Vector2(dist, panelMin.Y + framePadding.Y), ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudWhite), text);
            ImGui.GetWindowDrawList().PopClipRect();


            draw.AddRect(new Vector2(panelMin.X - framePadding.X, panelMin.Y), new Vector2(panelMax.X + framePadding.X, panelMax.Y), ImGui.GetColorU32(ImGuiCol.TableBorderLight), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, 1.0f);
        }

    }
}

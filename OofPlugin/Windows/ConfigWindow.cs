using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImPlotNET;
using System;
using System.IO;
using System.Numerics;

namespace OofPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private OofPlugin Plugin;

    private UIComponents components;
    private FileDialogManager fileDialogManager { get; }

    private float fallOptionsHeight = 0;
    private float deathOptionsHeight = 0;

    Vector4 headingColor = ImGuiColors.DalamudGrey;


    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(OofPlugin plugin) : base("oof options###OofPlugin:Window:Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(355, 720);
        SizeCondition = ImGuiCond.Always;

        Plugin = plugin;
        Configuration = plugin.Configuration;
        components = new UIComponents(plugin);

        fileDialogManager = new FileDialogManager
        {
            AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
        };
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        // i miss html/css

        AddLoadAudioUI();

        VolumeControlUI();

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Spacing();

        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Play sound on");

        // when self falls options
        var oofOnFall = Configuration.OofOnFall;
        UIComponents.SectionStart(fallOptionsHeight);
        UIComponents.SectionHeader("Fall damage (self only)", ref oofOnFall, () =>
        {
            Configuration.OofOnFall = oofOnFall;
            Configuration.Save();
        });
        if (!oofOnFall) ImGui.BeginDisabled();

        OnFallConfigUI();

        if (!oofOnFall) ImGui.EndDisabled();

        UIComponents.SectionEnd(ref fallOptionsHeight, oofOnFall ? ImGuiCol.PopupBg : ImGuiCol.TitleBg);
        ImGui.Spacing();
        // when people die options
        UIComponents.SectionStart(deathOptionsHeight);

        var oofOnDeath = Configuration.OofOnDeath;

        UIComponents.SectionHeader("Death", ref oofOnDeath, () => { Configuration.OofOnDeath = oofOnDeath; Configuration.Save(); });
        if (!oofOnDeath) ImGui.BeginDisabled();

        OnDeathConfigUI();

        if (!oofOnDeath) ImGui.EndDisabled();

        UIComponents.SectionEnd(ref deathOptionsHeight, oofOnDeath ? ImGuiCol.PopupBg : ImGuiCol.TitleBg);

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        /// watch video! --------
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExternalLinkSquareAlt, "Watch on Youtube")) OofPlugin.OpenVideo();
        var desc = "Hot Tip: You can Macro the /oofvideo command to\n for easy and streamlined access to this video.";
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(desc);

        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Learn about the history behind the Roblox Oof with Hbomberguy's Documentary");

        ImGui.Spacing();
        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Original Oof sound by Joey Kuras");

        ImGui.Spacing();


        fileDialogManager.Draw();
    }

    /// <summary>
    /// load audio interface
    /// </summary>
    private void AddLoadAudioUI()
    {
        var WindowPos = ImGui.GetWindowPos();
        var windowPadding = ImGui.GetStyle().WindowPadding;
        var em = ImGui.GetFontSize();
        var draw = ImGui.GetWindowDrawList();

        ImGui.AlignTextToFramePadding();

        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Sound file to play");
        ImGuiComponents.HelpMarker(
           "The audio that is triggered on death/fall damage");
        ImGui.SameLine(ImGui.GetWindowWidth() - UIComponents.CalcButtonSize(em) - windowPadding.X);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
        {
            Configuration.DefaultSoundImportPath = string.Empty;
            Configuration.Save();
            Plugin.SoundManager.LoadFile();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Reset audio file to default (oof)");


        if (UIComponents.IconCornerButton(FontAwesomeIcon.Play, "volume:play", ImDrawFlags.RoundCornersLeft))
        {
            Plugin.SoundManager.Play(Plugin.SoundManager.CancelToken.Token);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Play");

        ImGui.SameLine(0, 0);

        if (UIComponents.IconCornerButton(FontAwesomeIcon.Stop, "volume:stop", ImDrawFlags.RoundCornersRight))
        {
            Plugin.SoundManager.Stop();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Stop");

        ImGui.SameLine();
        var soundFileName = "Original Oof.wav";

        if (Configuration.DefaultSoundImportPath.Length > 0)
        {
            var formatString = Path.GetFileName(Configuration.DefaultSoundImportPath);
            if (formatString != null) soundFileName = formatString;

        }
        var browseText = "Upload Audio";
        var buttonWidth = UIComponents.CalcButtonSize(browseText) + ImGui.GetFontSize() * 1.4f;
        UIComponents.CustomDraggableText(soundFileName, buttonWidth);


        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FolderOpen, browseText))
        {
            void UpdatePath(bool success, string path)
            {
                if (!success || path.Length == 0) return;

                Configuration.DefaultSoundImportPath = path;
                Configuration.Save();
                Plugin.SoundManager.LoadFile();
            }

            fileDialogManager.OpenFileDialog("Open Audio File...", "Audio{.wav,.mp3,.aac,.wma}", UpdatePath);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("upload a custom audio file from your very own computer.");

    }
    private void VolumeControlUI()
    {
        var oofVolume = Configuration.Volume;
        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Volume");
        ImGui.AlignTextToFramePadding();
        UIComponents.ColorIcon(headingColor, FontAwesomeIcon.VolumeMute);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFontSize() * 1.6f);
        if (ImGui.SliderFloat("###volume", ref oofVolume, 0.0f, 1.0f))
        {
            Configuration.Volume = oofVolume;
            Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        UIComponents.ColorIcon(headingColor, FontAwesomeIcon.VolumeUp);
    }
    /// <summary>
    /// on fall options
    /// </summary>
    private void OnFallConfigUI()
    {
        ImGui.Columns(2);

        components.Checkbox("During combat###fall:combat", Configuration.OofOnFallBattle, (value) =>
        {
            Configuration.OofOnFallBattle = value;
        });

        ImGui.NextColumn();

        components.Checkbox("While mounted###fall:mounted", Configuration.OofOnFallMounted, (value) =>
        {
            Configuration.OofOnFallMounted = value;
        });

        ImGui.Columns(1);
    }
    /// <summary>
    /// on death options
    /// </summary>
    private void OnDeathConfigUI()
    {
        ImGui.Columns(2);

        components.Checkbox("During combat###death:combat", Configuration.OofOnDeathBattle, (value) =>
        {
            Configuration.OofOnDeathBattle = value;
        });

        ImGui.NextColumn();

        components.Checkbox("You dies###death:self", Configuration.OofOnDeathSelf, (value) =>
        {
            Configuration.OofOnDeathSelf = value;
        });

        components.Checkbox("Party member dies###death:party", Configuration.OofOnDeathParty, (value) =>
        {
            Configuration.OofOnDeathParty = value;
        });

        components.Checkbox("Alliance member dies###death:alliance", Configuration.OofOnDeathAlliance, (value) =>
        {
            Configuration.OofOnDeathAlliance = value;
        });

        ImGui.Columns(1);
        ImGui.Spacing();
        ImGui.Spacing();

        // distance based oof
        ImGui.Spacing();
        components.Checkbox("Distance Based Oof (DBO)###death:distance", Configuration.DistanceBasedOof, (value) =>
        {
            Configuration.DistanceBasedOof = value;
        });

        if (!Configuration.DistanceBasedOof) ImGui.BeginDisabled();


        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFontSize() * 2.4f);

        if (UIComponents.IconCornerButton(FontAwesomeIcon.Play, "dbo:play", ImDrawFlags.RoundCornersLeft))
        {
            _ = Plugin.SoundManager.TestDistanceAudio(Plugin.SoundManager.CancelToken.Token);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Test how distance affects sound");

        ImGui.SameLine(0, 0);

        if (UIComponents.IconCornerButton(FontAwesomeIcon.Stop, "dbo:stop", ImDrawFlags.RoundCornersRight))
        {
            Plugin.SoundManager.Stop();
        }

        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Lower volume based on how far someone dies from you, from 0 to 30 yalms");

        /// graph
        var steps = 800;
        var distancePoints = new float[steps];
        var volumemPoints = new float[steps];
        var step = 30f / steps;
        for (int i = 0; i < steps; i++)
        {
            distancePoints[i] = step * i;
            volumemPoints[i] = Plugin.SoundManager.VolumeFromDist(step * i);

        }
        if (ImPlot.BeginPlot("##dbo:graph", new Vector2(-1, 80), ImPlotFlags.CanvasOnly))
        {
            ImPlot.PushStyleColor(ImPlotCol.FrameBg, new Vector4(0, 0, 0, 0));
            ImPlot.PushStyleColor(ImPlotCol.AxisBgHovered, new Vector4(0, 0, 0, 0));
            ImPlot.PushStyleColor(ImPlotCol.AxisText, ImGuiColors.DalamudGrey);

            ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
            ImPlot.SetupLegend(ImPlotLocation.NorthEast, ImPlotLegendFlags.NoHighlightItem);


            ImPlot.SetupAxisZoomConstraints(ImAxis.X1, 30, 30);
            ImPlot.SetupAxisZoomConstraints(ImAxis.Y1, 1, 1);
            ImPlot.SetupAxesLimits(0, 30, 0, 1);
            ImPlot.SetupAxes(null, null, ImPlotAxisFlags.None, ImPlotAxisFlags.NoTickLabels);
            ImPlot.PopStyleColor();
            ImPlot.SetupFinish();

            ImPlot.PlotLine("volume", ref distancePoints[0], ref volumemPoints[0], steps);

            ImPlot.EndPlot();
        }
        ImGui.Columns(2);

        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Falloff Intensity");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var distanceFalloff = Configuration.DistanceFalloff;
        if (ImGui.SliderFloat("###death:distance:falloff", ref distanceFalloff, 0.0f, 1.0f))
        {
            Configuration.DistanceFalloff = distanceFalloff;
            Configuration.Save();
        }

        ImGui.NextColumn();
        ImGuiHelpers.SafeTextColoredWrapped(headingColor, "Minimum Volume");
        var distanceMinVolume = Configuration.DistanceMinVolume;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X);
        if (ImGui.SliderFloat("###death:distance:volume", ref distanceMinVolume, 0.0f, 1.0f))
        {
            Configuration.DistanceMinVolume = distanceMinVolume;
            Configuration.Save();
        }
        if (!Configuration.DistanceBasedOof) ImGui.EndDisabled();
        ImGui.Columns(1);
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


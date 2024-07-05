using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using OofPlugin.Windows;
using System;

//shoutout anna clemens

namespace OofPlugin;

public sealed class OofPlugin : IDalamudPlugin
{
    public string Name => "OOF";

    private const string oofCommand = "/oof";
    private const string oofSettings = "/oofsettings";
    private const string oofVideo = "/oofvideo";

    public readonly WindowSystem WindowSystem = new("OofPlugin");
    public Configuration Configuration { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    internal DeadPlayersList DeadPlayersList { get; init; }
    internal SoundManager SoundManager { get; init; }

    // i love global variables!!!! the more global the more globaly it gets

    //check for fall
    private float prevPos { get; set; } = 0;
    private float prevVel { get; set; } = 0;
    private float distJump { get; set; } = 0;
    private bool wasJumping { get; set; } = false;


    public OofPlugin(IDalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);

        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface);

        SoundManager = new SoundManager(this);
        DeadPlayersList = new DeadPlayersList();


        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        Dalamud.PluginInterface.UiBuilder.Draw += DrawUI;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Dalamud.Framework.Update += FrameworkOnUpdate;

        Dalamud.CommandManager.AddHandler(oofCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "play oof sound"
        });
        Dalamud.CommandManager.AddHandler(oofSettings, new CommandInfo(OnCommand)
        {
            HelpMessage = "change oof settings"
        });
        Dalamud.CommandManager.AddHandler(oofVideo, new CommandInfo(OnCommand)
        {
            HelpMessage = "open Hbomberguy video on OOF.mp3"
        });

    }

    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    private void OnCommand(string command, string args)
    {
        if (command == oofCommand) SoundManager.Play(SoundManager.CancelToken.Token);
        if (command == oofSettings) ToggleConfigUI();
        if (command == oofVideo) OpenVideo();

    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (Dalamud.ClientState == null || Dalamud.ClientState.LocalPlayer == null) return;
        // dont run btwn moving areas          
        if (Dalamud.Condition[ConditionFlag.BetweenAreas] || Dalamud.Condition[ConditionFlag.BetweenAreas51] || Dalamud.Condition[ConditionFlag.BeingMoved]) return;
        if (Dalamud.Condition[ConditionFlag.WatchingCutscene] || Dalamud.Condition[ConditionFlag.WatchingCutscene78]) return;


        try
        {
            if (Configuration.OofOnFall) CheckFallen();
            if (Configuration.OofOnDeath) CheckDeath();
        }
        catch (Exception e)
        {
            Dalamud.Log.Error("failed to check for oof condition:", e.Message);
        }
    }

    /// <summary>
    /// check if player has died during alliance, party, and self.
    /// this may be the worst if statement chain i have made
    /// </summary>
    private void CheckDeath()
    {

        if (!Configuration.OofOnDeathBattle && Dalamud.Condition[ConditionFlag.InCombat]) return;

        // if not in party
        if (Configuration.OofOnDeathSelf && (Dalamud.PartyList == null || Dalamud.PartyList.Length == 0))
        {
            DeadPlayersList.AddRemoveDeadPlayer(Dalamud.ClientState.LocalPlayer!);
            return;
        }
        // if in alliance
        if (Configuration.OofOnDeathAlliance && Dalamud.PartyList.Length == 8 && Dalamud.PartyList.GetAllianceMemberAddress(0) != IntPtr.Zero) // the worst "is alliance" check
        {
            try
            {
                for (int i = 0; i < 16; i++)
                {
                    var allianceMemberAddress = Dalamud.PartyList.GetAllianceMemberAddress(i);
                    if (allianceMemberAddress == IntPtr.Zero) throw new NullReferenceException("allience member address is null");

                    var allianceMember = Dalamud.PartyList.CreateAllianceMemberReference(allianceMemberAddress) ?? throw new NullReferenceException("allience reference is null");
                    DeadPlayersList.AddRemoveDeadPlayer(allianceMember);
                }
            }
            catch (Exception e)
            {
                Dalamud.Log.Error("failed alliance check", e.Message);
            }
        }
        //if in party
        if (Configuration.OofOnDeathParty)
        {
            foreach (var member in Dalamud.PartyList)
            {
                DeadPlayersList.AddRemoveDeadPlayer(member, member.Territory.Id == Dalamud.ClientState.TerritoryType);
            }
        }
    }

    /// <summary>
    /// check if player has taken fall damage (brute force way)
    /// </summary>
    private void CheckFallen()
    {
        if (!Configuration.OofOnFallBattle && Dalamud.Condition[ConditionFlag.InCombat]) return;
        if (!Configuration.OofOnFallMounted && (Dalamud.Condition[ConditionFlag.Mounted] || Dalamud.Condition[ConditionFlag.Mounted2])) return;

        var isJumping = Dalamud.Condition[ConditionFlag.Jumping];
        var pos = Dalamud.ClientState!.LocalPlayer!.Position.Y;
        var velocity = prevPos - pos;

        if (isJumping && !wasJumping)
        {
            if (prevVel < 0.17) distJump = pos; //started falling
        }
        else if (wasJumping && !isJumping)  // stopped falling
        {
            if (distJump - pos > 9.60) SoundManager.Play(SoundManager.CancelToken.Token); // fell enough to take damage // i guessed and checked this distance value btw
        }

        // set position for next timestep
        prevPos = pos;
        prevVel = velocity;
        wasJumping = isJumping;
    }


    /// <summary>
    /// open the hbomberguy video on oof
    /// </summary>
    public static void OpenVideo()
    {
        Util.OpenLink("https://www.youtube.com/watch?v=0twDETh6QaI");
    }

    /// <summary>
    /// dispose
    /// </summary>
    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        SoundManager.Dispose();

        Dalamud.CommandManager.RemoveHandler(oofCommand);
        Dalamud.CommandManager.RemoveHandler(oofSettings);
        Dalamud.CommandManager.RemoveHandler(oofVideo);

        Dalamud.Framework.Update -= FrameworkOnUpdate;

    }
}

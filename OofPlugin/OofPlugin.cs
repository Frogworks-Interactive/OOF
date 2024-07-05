using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using OofPlugin.Windows;
using System;
using System.Linq;

//shoutout anna clemens

namespace OofPlugin;

public sealed class OofPlugin : IDalamudPlugin
{
    public string Name => "OOF";

    private const string oofCommand = "/oof";
    private const string oofSettings = "/oofsettings";
    private const string oofVideo = "/oofvideo";
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("OofPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    internal DeadPlayersList DeadPlayersList { get; init; }
    internal SoundManager SoundManager { get; init; }

    // i love global variables!!!! the more global the more globaly it gets

    //check for fall
    private float prevPos { get; set; } = 0;
    private float prevVel { get; set; } = 0;
    private float distJump { get; set; } = 0;
    private bool wasJumping { get; set; } = false;


    public OofPlugin()
    {

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        SoundManager = new SoundManager(this);
        DeadPlayersList = new DeadPlayersList();


        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Framework.Update += FrameworkOnUpdate;

        CommandManager.AddHandler(oofCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "play oof sound"
        });
        CommandManager.AddHandler(oofSettings, new CommandInfo(OnCommand)
        {
            HelpMessage = "change oof settings"
        });
        CommandManager.AddHandler(oofVideo, new CommandInfo(OnCommand)
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
        if (ClientState == null || ClientState.LocalPlayer == null) return;
        // dont run btwn moving areas          
        if (Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] || Condition[ConditionFlag.BeingMoved]) return;
        if (Condition[ConditionFlag.WatchingCutscene] || Condition[ConditionFlag.WatchingCutscene78]) return;


        try
        {
            if (Configuration.OofOnFall) CheckFallen();
            if (Configuration.OofOnDeath) CheckDeath();
        }
        catch (Exception e)
        {
            PluginLog.Error("failed to check for oof condition:", e.Message);
        }
    }

    /// <summary>
    /// check if player has died during alliance, party, and self.
    /// this may be the worst if statement chain i have made
    /// </summary>
    private void CheckDeath()
    {

        if (!Configuration.OofOnDeathBattle && Condition[ConditionFlag.InCombat]) return;

        if (PartyList != null && PartyList.Any())
        {
            if (Configuration.OofOnDeathAlliance && PartyList.Length == 8 && PartyList.GetAllianceMemberAddress(0) != IntPtr.Zero) // the worst "is alliance" check
            {
                try
                {
                    for (int i = 0; i < 16; i++)
                    {
                        var allianceMemberAddress = PartyList.GetAllianceMemberAddress(i);
                        if (allianceMemberAddress == IntPtr.Zero) throw new NullReferenceException("allience member address is null");

                        var allianceMember = PartyList.CreateAllianceMemberReference(allianceMemberAddress) ?? throw new NullReferenceException("allience reference is null");
                        DeadPlayersList.AddRemoveDeadPlayer(allianceMember);
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error("failed alliance check", e.Message);
                }
            }
            if (Configuration.OofOnDeathParty)
            {
                foreach (var member in PartyList)
                {
                    DeadPlayersList.AddRemoveDeadPlayer(member, member.Territory.Id == ClientState.TerritoryType);
                }
            }

        }
        else
        {
            if (!Configuration.OofOnDeathSelf) return;
            DeadPlayersList.AddRemoveDeadPlayer(ClientState.LocalPlayer!);
        }

    }

    /// <summary>
    /// check if player has taken fall damage (brute force way)
    /// </summary>
    private void CheckFallen()
    {
        if (!Configuration.OofOnFallBattle && Condition[ConditionFlag.InCombat]) return;
        if (!Configuration.OofOnFallMounted && (Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2])) return;

        var isJumping = Condition[ConditionFlag.Jumping];
        var pos = ClientState!.LocalPlayer!.Position.Y;
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

        CommandManager.RemoveHandler(oofCommand);
        CommandManager.RemoveHandler(oofSettings);
        CommandManager.RemoveHandler(oofVideo);

        Framework.Update -= FrameworkOnUpdate;

    }
}

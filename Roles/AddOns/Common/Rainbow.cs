using static TOHFE.Options;

namespace TOHFE.Roles.AddOns.Common;

// https://github.com/Yumenopai/TownOfHost_Y/blob/main/Roles/Crewmate/Y/Rainbow.cs
public static class Rainbow
{
    private const int Id = 27700;
    public static OptionItem CrewCanBeRainbow;
    public static OptionItem ImpCanBeRainbow;
    public static OptionItem NeutralCanBeRainbow;
    private static OptionItem RainbowColorChangeCoolDown;
    private static OptionItem ChangeInCamouflage;

    public static bool isEnabled = false;
    public static long LastColorChange;
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rainbow, canSetNum: true, tab: TabGroup.Addons);
        CrewCanBeRainbow = BooleanOptionItem.Create(Id + 10, "CrewCanBeRainbow", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
        ImpCanBeRainbow = BooleanOptionItem.Create(Id + 11, "ImpCanBeRainbow", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
        NeutralCanBeRainbow = BooleanOptionItem.Create(Id + 12, "NeutralCanBeRainbow", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
        RainbowColorChangeCoolDown = IntegerOptionItem.Create(Id + 13, "RainbowColorChangeCoolDown", new(1, 100, 1), 3, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
        ChangeInCamouflage = BooleanOptionItem.Create(Id + 14, "RainbowInCamouflage", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
    }
    public static void Init()
    {
        LastColorChange = Utils.GetTimeStamp();
        isEnabled = false;
    }
    public static void Add()
    {
        isEnabled = true;
    }
    public static void OnFixedUpdate()
    {
        if (Camouflage.IsCamouflage && !ChangeInCamouflage.GetBool()) return;

        if (LastColorChange + RainbowColorChangeCoolDown.GetInt() <= Utils.GetTimeStamp())
        {
            LastColorChange = Utils.GetTimeStamp();
            ChangeAllColor();
        }

    }
    private static void ChangeAllColor()
    {
        var sender = CustomRpcSender.Create("Rainbow Sender");
        // When the player is in the vent and changes color, he gets stuck
        foreach (var player in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Rainbow) && x.IsAlive() && !x.inMovingPlat && !x.inVent && !x.walkingToVent && !x.onLadder))
        {
            int color = PickRandomColor();
            player.SetColor(color);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetColor)
                .Write(player.Data.NetId)
                .Write((byte)color)
                .EndRpc();
        }
        sender.SendMessage();
    }
    private static int PickRandomColor()
    {
        //make this function so we may extend it in the future
        return IRandom.Instance.Next(0, 18);
    }
}

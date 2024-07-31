using AmongUs.GameOptions;
using static TOHFE.Options;

namespace TOHFE.Roles.AddOns.Common;

public class Reach
{
    private const int Id = 23700;

    public static CustomRoles IsReach = CustomRoles.Reach; // Used to find "references" of this addon.
    
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reach, canSetNum: true);
    }
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetInt(Int32OptionNames.KillDistance, 2);
}
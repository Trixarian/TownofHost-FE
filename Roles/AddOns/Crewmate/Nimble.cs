using static TOHFE.Options;

namespace TOHFE.Roles.AddOns.Crewmate;

public class Nimble
{
    private const int Id = 19700;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Nimble, canSetNum: true, tab: TabGroup.Addons);
    }
}
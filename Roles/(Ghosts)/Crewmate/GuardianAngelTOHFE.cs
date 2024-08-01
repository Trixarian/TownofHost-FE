using AmongUs.GameOptions;
using TOHFE.Roles.Core;
using static TOHFE.Options;

namespace TOHFE.Roles._Ghosts_.Crewmate;

internal class GuardianAngelTOHFE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 20900;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanillaGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem ProtectDur;
    private static OptionItem ImpVis;

    public static readonly Dictionary<byte, long> PlayerShield = [];
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.GuardianAngelTOHFE);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.GuardianAngelBase_ProtectCooldown, new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHFE])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDur = IntegerOptionItem.Create(Id + 11, GeneralOption.GuardianAngelBase_ProtectionDuration, new(1, 120, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHFE])
            .SetValueFormat(OptionFormat.Seconds);
        ImpVis = BooleanOptionItem.Create(Id + 12, GeneralOption.GuardianAngelBase_ImpostorsCanSeeProtect, true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHFE]);
    }
    public override void Init()
    {
        PlayerShield.Clear();
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnOthersFixUpdate);
        PlayerIds.Add(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = ProtectDur.GetFloat();
        AURoleOptions.ImpostorsCanSeeProtect = ImpVis.GetBool();
    }
    public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        if (!PlayerShield.ContainsKey(target.PlayerId))
        {
            PlayerShield.Add(target.PlayerId, Utils.GetTimeStamp());
        }
        else
        {
            PlayerShield[target.PlayerId] = Utils.GetTimeStamp();
        }
        return true;
    }
    public override void OnOtherTargetsReducedToAtoms(PlayerControl target)
    {
        if (PlayerShield.ContainsKey(target.PlayerId))
            PlayerShield.Remove(target.PlayerId);
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (PlayerShield.ContainsKey(target.PlayerId))
        {
            if (ImpVis.GetBool())
            {
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
            }
            return true;
        }
        return false;
    }

    private void OnOthersFixUpdate(PlayerControl player)
    {
        if (PlayerShield.ContainsKey(player.PlayerId) && PlayerShield[player.PlayerId] + ProtectDur.GetInt() <= Utils.GetTimeStamp())
        {
            PlayerShield.Remove(player.PlayerId);
        }
    }
}

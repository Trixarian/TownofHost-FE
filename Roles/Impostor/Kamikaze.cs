using Hazel;
using TOHFE.Modules;
using TOHFE.Modules.Rpc;
using TOHFE.Roles.Core;
using TOHFE.Roles.Double;
using static TOHFE.Options;
using static TOHFE.Translator;

namespace TOHFE.Roles.Impostor;

internal class Kamikaze : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Kamikaze;
    private const int Id = 26900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Kamikaze);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptMaxMarked;
    private static OptionItem CanKillTNA;

    private readonly HashSet<byte> KamikazedList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kamikaze);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxMarked = IntegerOptionItem.Create(Id + 11, "KamikazeMaxMarked", new(1, 14, 1), 14, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
           .SetValueFormat(OptionFormat.Times);
        CanKillTNA = BooleanOptionItem.Create(Id + 12, "CanKillTNA", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze]);

    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(OptMaxMarked.GetInt());

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => KamikazedList.Contains(seen.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), "∇") : string.Empty;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), GetString("KamikazeHostage")));
            return false;
        }

        return killer.CheckDoubleTrigger(target, () =>
        {
            if (killer.GetAbilityUseLimit() >= 1 && !KamikazedList.Contains(target.PlayerId))
            {
                KamikazedList.Add(target.PlayerId);
                killer.RpcGuardAndKill(killer);
                killer.SetKillCooldown(KillCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer);
                killer.RpcRemoveAbilityUse();
            }
            else
            {
                killer.RpcMurderPlayer(target);
            }
        });

    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (_Player == null || _Player.IsDisconnected()) return;

        foreach (var BABUSHKA in KamikazedList)
        {
            var pc = Utils.GetPlayerById(BABUSHKA);
            if (!pc.IsAlive()) continue;
            if (pc.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool()) continue;

            pc.SetDeathReason(PlayerState.DeathReason.Targeted);
            if (!inMeeting)
            {
                pc.RpcMurderPlayer(pc);
            }
            else
            {
                pc.RpcExileV2();
                Main.PlayerStates[pc.PlayerId].SetDead();
                pc.Data.IsDead = true;
            }
            pc.SetRealKiller(_Player);
        }
        KamikazedList.Clear();
        SendRPC();
    }

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || !exileIds.Contains(_Player.PlayerId)) return;
        var deathList = new List<byte>();
        var death = _Player;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (KamikazedList.Contains(pc.PlayerId))
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(death);
                    deathList.Add(pc.PlayerId);
                }
            }
        }
        KamikazedList.Clear();
        SendRPC();
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Targeted, [.. deathList]);
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.WritePacked(KamikazedList.Count);
        foreach (var playerId in KamikazedList)
        {
            writer.Write(playerId);
        }
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var count = reader.ReadPackedInt32();
        KamikazedList.Clear();
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                KamikazedList.Add(reader.ReadByte());
            }
        }
    }
}


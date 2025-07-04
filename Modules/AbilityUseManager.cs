﻿using System;
using TOHFE.Modules.Rpc;

namespace TOHFE.Modules;

public static class AbilityUseManager
{
    private static readonly Dictionary<byte, float> AbilityUseLimit = [];

    public static void Initializate()
    {
        AbilityUseLimit.Clear();
    }

    public static float GetAbilityUseLimit(this PlayerControl pc) => AbilityUseLimit.GetValueOrDefault(pc.PlayerId, float.NaN);
    public static float GetAbilityUseLimit(this byte playerId) => AbilityUseLimit.GetValueOrDefault(playerId, float.NaN);

    public static void RpcRemoveAbilityUse(this PlayerControl pc, bool log = true)
    {
        float current = pc.GetAbilityUseLimit();
        if (float.IsNaN(current) || current <= 0f) return;
        pc.SetAbilityUseLimit(current - 1, log: log);
    }

    public static void RpcIncreaseAbilityUseLimitBy(this PlayerControl pc, float get, bool log = true)
    {
        float current = pc.GetAbilityUseLimit();
        if (float.IsNaN(current)) return;
        pc.SetAbilityUseLimit(current + get, log: log);
    }

    public static void SetAbilityUseLimit(this PlayerControl pc, float limit, bool rpc = true, bool log = true) => pc.PlayerId.SetAbilityUseLimit(limit, rpc, log);

    public static void SetAbilityUseLimit(this byte playerId, float limit, bool rpc = true, bool log = true)
    {
        limit = (float)Math.Round(limit, 1);

        if (float.IsNaN(limit) || limit is < 0f or > 1000f || AbilityUseLimit.TryGetValue(playerId, out var beforeLimit) && Math.Abs(beforeLimit - limit) < 0.01f) return;

        AbilityUseLimit[playerId] = limit;

        var player = playerId.GetPlayer();
        if (AmongUsClient.Instance.AmHost && player.IsNonHostModdedClient() && rpc)
        {
            var message = new RpcSyncAbilityUseLimit(PlayerControl.LocalPlayer.NetId, playerId, limit);
            RpcUtils.LateBroadcastReliableMessage(message);
        }

        Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
        if (log) Logger.Info($" {player.GetNameWithRole()} => {Math.Round(limit, 1)}", "SetAbilityUseLimit");
    }
}

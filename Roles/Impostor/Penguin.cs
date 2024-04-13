﻿using Hazel;
using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Impostor/Penguin.cs
namespace TOHE.Roles.Impostor;

internal class Penguin : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 27500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem OptionAbductTimerLimit;
    private static OptionItem OptionMeetingKill;


    public static PlayerControl AbductVictim;
    private static float AbductTimer;
    private static float AbductTimerLimit;
    private static bool stopCount;
    private static bool MeetingKill;

    // Measures to prevent the opponent who is about to be killed during abduction from using their abilities

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Penguin, 1);
        OptionAbductTimerLimit = FloatOptionItem.Create(Id + 11, "PenguinAbductTimerLimit", new(1f, 20f, 1f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Penguin])
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(Id + 12, "PenguinMeetingKill", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Penguin]);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();

        playerIdList.Add(playerId);

        AbductTimer = 255f;
        stopCount = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : AbductTimerLimit;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Penguin);
        writer.Write(AbductVictim?.PlayerId ?? 255);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var victim = reader.ReadByte();

        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }
        else
        {
            AbductVictim = Utils.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
        }
    }

    private static void AddVictim(PlayerControl penguin, PlayerControl target)
    {
        //Prevent using of moving platform??
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        penguin?.MarkDirtySettings();
        penguin?.RpcResetAbilityCooldown();
        SendRPC();
    }
    private static void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            //PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim = null;
        }
        //MyState.CanUseMovingPlatform = true;
        AbductTimer = 255f;
        Utils.GetPlayerById(playerIdList.First())?.MarkDirtySettings();
        Utils.GetPlayerById(playerIdList.First())?.RpcResetAbilityCooldown();
        SendRPC();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        bool doKill = true;
        if (AbductVictim != null)
        {
            if (target != AbductVictim)
            {
                // During an abduction, only the abductee can be killed.
                killer?.RpcMurderPlayer(AbductVictim);
                killer?.ResetKillCooldown();
                doKill = false;
            }
            RemoveVictim();
        }
        else
        {
            doKill = false;
            AddVictim(killer, target);
        }
        return doKill;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        // not should shapeshifted
        resetCooldown = false;
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(AbductVictim != null ? GetString("KillButtonText") : GetString("PenguinKillButtonText"));
        hud.AbilityButton?.OverrideText(GetString("PenguinTimerText"));
        hud.AbilityButton?.ToggleVisible(AbductVictim != null);
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        stopCount = true;
        // If you meet a meeting with time running out, kill it even if you're on a ladder.
        if (AbductVictim != null && AbductTimer <= 0f)
        {
            Utils.GetPlayerById(playerIdList.First())?.RpcMurderPlayer(AbductVictim);
        }
        if (MeetingKill)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (AbductVictim == null) return;
            Utils.GetPlayerById(playerIdList.First())?.RpcMurderPlayer(AbductVictim);
            RemoveVictim();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (GameStates.AirshipIsActive) return;

        //Maps other than Airship
        RestartAbduct();
    }
    public static void OnSpawnAirship()
    {
        RestartAbduct();
    }
    private static void RestartAbduct()
    {
        if (!HasEnabled) return;
        if (AbductVictim != null)
        {
            Utils.GetPlayerById(playerIdList.First())?.MarkDirtySettings();
            Utils.GetPlayerById(playerIdList.First())?.RpcResetAbilityCooldown();
            stopCount = false;
        }
    }

    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        if (AbductVictim != null)
        {
            physics.RpcBootFromVent(ventId);
        }
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (AbductVictim != null)
        {
            if (physics.myPlayer.PlayerId == AbductVictim.PlayerId)
            {
                physics.RpcBootFromVent(ventId);
                return true;
            }
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl penguin)
    {
        if (GameStates.IsMeeting) return;

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!penguin.IsAlive() || !AbductVictim.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f && !penguin.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                // Set IsDead to true first (prevents ladder chase)
                AbductVictim.Data.IsDead = true;
                GameData.Instance.SetDirty();
                // If the penguin himself is on a ladder, kill him after getting off the ladder.
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;
                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        //Host side
                        abductVictim.NetTransform.SnapTo(penguin.transform.position, (ushort)sId);
                        penguin.MurderPlayer(abductVictim, ExtendedPlayerControl.ResultFlags);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {  
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(penguin.transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }
                            sender.EndRpc();
                            sender.AutoStartRpc(penguin.NetId, (byte)RpcCalls.MurderPlayer);
                            {
                                sender.WriteNetObject(abductVictim);
                                sender.Write((int)ExtendedPlayerControl.ResultFlags);
                            }
                            sender.EndRpc();
                        }
                        sender.SendMessage();

                    }, 0.3f, "PenguinMurder");
                    RemoveVictim();
                }
            }
            // SnapToRPC does not work for players on top of the ladder, and only the host behaves differently, so teleporting is not done uniformly.
            else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                var position = penguin.transform.position;
                if (penguin.PlayerId != 0)
                {
                    AbductVictim.RpcTeleport(position, sendInfoInLogs: false);
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        AbductVictim?.RpcTeleport(position, sendInfoInLogs: false);
                    }
                    , 0.25f, "");
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            penguin.RpcResetAbilityCooldown();
        }
    }
}

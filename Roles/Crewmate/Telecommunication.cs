using System;
using System.Text;
using TOHFE.Roles.Neutral;
using UnityEngine;
using static TOHFE.Utils;
using static TOHFE.Translator;
using AmongUs.GameOptions;

namespace TOHFE.Roles.Crewmate;

internal class Telecommunication : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 12500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem CanCheckCamera;
    private static OptionItem CanVent;
    
    private static bool IsAdminWatch;
    private static bool IsVitalWatch;
    private static bool IsDoorLogWatch;
    private static bool IsCameraWatch;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Telecommunication);
        CanCheckCamera = BooleanOptionItem.Create(Id + 10, "CanCheckCamera", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Telecommunication]);
        CanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Telecommunication]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        IsAdminWatch = false;
        IsVitalWatch = false;
        IsDoorLogWatch = false;
        IsCameraWatch = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static bool CanUseVent() => CanVent.GetBool();

    private static int Count = 0;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        Count--; if (Count > 0) return; Count = 5;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (Pelican.IsEaten(pc.PlayerId) || pc.inVent) continue;
            try
            {
                Vector2 PlayerPos = pc.transform.position;
                var mapId = GetActiveMapId();
                var mapName = (MapNames)mapId;

                switch (mapId)
                {
                    case 0:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 1:
                        if (!Options.DisableMiraHQAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableMiraHQDoorLog.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 2:
                        if (!Options.DisablePolusAdmin.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance(mapName);
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        }
                        if (!Options.DisablePolusCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisablePolusVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 3:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["DleksAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["DleksCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 4:
                        if (!Options.DisableAirshipCockpitAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipRecordsAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 5:
                        if (!Options.DisableFungleBinoculars.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["FungleCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableFungleVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["FungleVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "AntiAdmin");
            }
        }

        var isChange = false;

        isChange |= IsAdminWatch != Admin;
        IsAdminWatch = Admin;
        isChange |= IsVitalWatch != Vital;
        IsVitalWatch = Vital;
        isChange |= IsDoorLogWatch != DoorLog;
        IsDoorLogWatch = DoorLog;
        if (CanCheckCamera.GetBool())
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            foreach (var pc in playerIdList)
            {
                var antiAdminer = GetPlayerById(pc);
                NotifyRoles(SpecifySeer: antiAdminer, ForceLoop: false);
            }
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seer.PlayerId != seen.PlayerId || isForMeeting) return string.Empty;

        StringBuilder sb = new();
        if (IsAdminWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), "★")).Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), GetString("AdminWarning")));
        if (IsVitalWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), "★")).Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), GetString("VitalsWarning")));
        if (IsDoorLogWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), "★")).Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), GetString("DoorlogWarning")));
        if (IsCameraWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), "★")).Append(ColorString(GetRoleColor(CustomRoles.Telecommunication), GetString("CameraWarning")));

        return sb.ToString();
    }
}
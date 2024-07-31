using AmongUs.Data;
using Discord;
using System;

namespace TOHFE.Patches
{
    // Originally from Town of Us Rewritten, by Det
    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    public class DiscordRPC
    {
        private static string lobbycode = "";
        private static string region = "";
        public static void Prefix([HarmonyArgument(0)] Activity activity)
        {
            if (activity == null) return;

            var details = $"TOHFE v{Main.PluginDisplayVersion}";
            activity.Details = details;

            try
            {
                if (activity.State != "In Menus")
                {
                    if (!DataManager.Settings.Gameplay.StreamerMode)
                    {
                        int maxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
                        if (GameStates.IsLobby)
                        {
                            lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
                            region = Utils.GetRegionName();
                        }

                        if (lobbycode != "" && region != "")
                        {
                            if (GameStates.IsNormalGame)
                                details = $"TOHFE - {lobbycode} ({region})";

                            else if (GameStates.IsHideNSeek)
                                details = $"TOHFE Hide & Seek - {lobbycode} ({region})";
                        }

                        activity.Details = details;
                    }
                    else
                    {
                        if (GameStates.IsNormalGame)
                            details = $"TOHFE v{Main.PluginDisplayVersion}";

                        else if (GameStates.IsHideNSeek)
                            details = $"TOHFE v{Main.PluginDisplayVersion} - Hide & Seek";

                        else details = $"TOHFE v{Main.PluginDisplayVersion}";

                        activity.Details = details;
                    }
                }
            }

            catch (ArgumentException ex)
            {
                Logger.Error("Error in updating discord rpc", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                details = $"TOHFE v{Main.PluginDisplayVersion}";
                activity.Details = details;
            }
        }
    }
}
using HarmonyLib;
using Steamworks;

namespace BingBox.Network
{
    [HarmonyPatch(typeof(SteamMatchmaking))]
    public static class SteamMatchmakingPatch
    {
        [HarmonyPatch(nameof(SteamMatchmaking.LeaveLobby))]
        [HarmonyPostfix]
        public static void LeaveLobbyPostfix(CSteamID steamIDLobby)
        {
            if (Plugin.Instance != null && Plugin.Instance.SteamLobbyManager != null)
            {
                Plugin.Instance.SteamLobbyManager.OnLocalUserLeftLobby();
            }
        }
    }
}

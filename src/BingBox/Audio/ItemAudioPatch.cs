using HarmonyLib;
using BingBox.Logging;

namespace BingBox.Audio;

[HarmonyPatch(typeof(Item))]
public static class ItemAudioPatch
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void ItemStart_Postfix(Item __instance)
    {
        if (BingBoxAudioManager.Instance != null && BingBoxAudioManager.IsTargetItem(__instance.name))
        {
            BingBoxAudioManager.Instance.RegisterItem(__instance);
        }
    }

    [HarmonyPatch("OnDestroy")]
    [HarmonyPrefix]
    public static void ItemOnDestroy_Prefix(Item __instance)
    {
        if (BingBoxAudioManager.Instance != null && BingBoxAudioManager.IsTargetItem(__instance.name))
        {
            BingBoxAudioManager.Instance.UnregisterItem(__instance);
        }
    }
}

using HarmonyLib;
using LunarFramework.Patching;
using Verse.Profile;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MemoryUtility))]
internal static class Patch_Verse_MemoryUtility
{
    [HarmonyPostfix]
    [HarmonyPatch("ClearAllMapsAndWorld")]
    private static void ClearAllMapsAndWorld()
    {
        ExtensionUtils.ClearCaches();
        WorldTileInfo.RemoveCache();
    }
}

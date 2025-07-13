#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;

namespace GeologicalLandforms.Patches;

/// <summary>
/// Transiently filter out landmarks which have been disabled by the user via customization settings.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldLandmarks))]
internal static class Patch_RimWorld_WorldLandmarks
{
    [HarmonyPostfix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch("get_Item", typeof(PlanetTile))]
    private static void GetItem_Postfix(ref Landmark __result)
    {
        if (__result != null && TileMutatorsCustomization.Enabled && TileMutatorsCustomization.IsLandmarkDisabled(__result.def))
        {
            __result = null;
        }
    }
}

#endif

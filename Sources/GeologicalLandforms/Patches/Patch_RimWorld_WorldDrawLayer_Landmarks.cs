#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;

namespace GeologicalLandforms.Patches;

/// <summary>
/// Hide landmarks on the world map which have been disabled by the user via customization settings.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldDrawLayer_Landmarks))]
internal static class Patch_RimWorld_WorldDrawLayer_Landmarks
{
    [HarmonyPrefix]
    [HarmonyPatch("DrawStandard")]
    private static bool DrawStandard_Prefix(Landmark landmark)
    {
        return !TileMutatorsCustomizationCache.IsLandmarkDisabled(landmark.def);
    }
}

#endif

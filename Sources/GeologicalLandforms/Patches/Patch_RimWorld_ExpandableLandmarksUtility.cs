#if RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

/// <summary>
/// Hide expandable items for landmarks which have been disabled by the user via customization settings.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(ExpandableLandmarksUtility))]
internal static class Patch_RimWorld_ExpandableLandmarksUtility
{
    private static readonly Type Self = typeof(Patch_RimWorld_ExpandableLandmarksUtility);

    [HarmonyTranspiler]
    [HarmonyPatch("get_LandmarksToShow")]
    private static IEnumerable<CodeInstruction> GetLandmarksToShow_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("LandmarksToShow")
            .MatchCall(typeof(List<PlanetTile>), "Add")
            .Replace(OpCodes.Call, AccessTools.Method(Self, nameof(AddIfNotDisabled)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static void AddIfNotDisabled(List<PlanetTile> list, PlanetTile tile)
    {
        if (Find.World.landmarks.landmarks.TryGetValue(tile, out var landmark))
        {
            if (landmark != null && !TileMutatorsCustomization.IsLandmarkDisabled(landmark.def))
            {
                list.Add(tile);
            }
        }
    }
}

#endif

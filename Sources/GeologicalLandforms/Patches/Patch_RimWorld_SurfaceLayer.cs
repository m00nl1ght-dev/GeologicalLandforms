#if RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(SurfaceLayer))]
internal static class Patch_RimWorld_SurfaceLayer
{
    private static readonly Type Self = typeof(Patch_RimWorld_SurfaceLayer);

    [HarmonyTranspiler]
    [HarmonyPatch("SerializeMutators")]
    private static IEnumerable<CodeInstruction> SerializeMutators_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("SerializeMutators")
            .MatchCall(typeof(Tile), "get_Mutators").Remove()
            .MatchCall(typeof(Enumerable), nameof(Enumerable.ToList), null, [typeof(TileMutatorDef)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(TileMutatorsForSaving)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static List<TileMutatorDef> TileMutatorsForSaving(Tile tile)
    {
        return tile.mutatorsNullable ?? [];
    }
}

#endif

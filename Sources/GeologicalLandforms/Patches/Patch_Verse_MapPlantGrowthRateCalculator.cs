using System;
using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapPlantGrowthRateCalculator))]
internal static class Patch_Verse_MapPlantGrowthRateCalculator
{
    internal static readonly Type Self = typeof(Patch_Verse_MapPlantGrowthRateCalculator);

    [HarmonyTranspiler]
    [HarmonyPatch("BuildFor", typeof(Map))]
    private static IEnumerable<CodeInstruction> BuildFor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("BuildFor")
            .MatchCall(typeof(Map), "get_Tile").Remove()
            .Insert(CodeInstruction.Call(Self, nameof(TileForMap)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static int TileForMap(Map map)
    {
        return map.Tile < 0 && map.Parent is PocketMapParent { sourceMap.Tile: >= 0 } parent ? parent.sourceMap.Tile : map.Tile;
    }
}

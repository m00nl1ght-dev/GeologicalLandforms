using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_ElevationFertility))]
internal static class Patch_RimWorld_GenStep_ElevationFertility
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyBefore("com.configurablemaps.rimworld.mod")]
    private static bool Generate_Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (!Landform.AnyGenerating) return true;

        var elevationModule = Landform.GetFeatureScaled(l => l.OutputElevation?.Get());
        var fertilityModule = Landform.GetFeatureScaled(l => l.OutputFertility?.Get());

        if (elevationModule == null && fertilityModule == null) return true;

        var elevationSeed = Rand.Range(0, int.MaxValue);
        var fertilitySeed = Rand.Range(0, int.MaxValue);

        #if RW_1_5_OR_GREATER
        if (map.generatorDef.isUnderground) elevationModule ??= GridFunction.Of(1d);
        #endif

        elevationModule ??= NodeInputBase.BuildVanillaElevationGrid(Landform.GeneratingTile, elevationSeed);
        fertilityModule ??= NodeInputBase.BuildVanillaFertilityGrid(Landform.GeneratingTile, fertilitySeed);

        var elevation = MapGenerator.Elevation;
        var fertility = MapGenerator.Fertility;

        foreach (var cell in map.AllCells)
        {
            elevation[cell] = (float) elevationModule.ValueAt(cell.x, cell.z);
            fertility[cell] = (float) fertilityModule.ValueAt(cell.x, cell.z);
        }

        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.First)]
    private static IEnumerable<CodeInstruction> Generate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var self = typeof(Patch_RimWorld_GenStep_ElevationFertility);

        var brfalseSkip = new CodeInstruction(OpCodes.Brfalse);

        var pattern = TranspilerPattern.Build("ShouldUseVanillaMountainGeneration")
            .Match(OpCodes.Ldarg_1).Keep()
            .MatchCall(typeof(Map), "get_TileInfo").Keep()
            .MatchLoad(typeof(Tile), "hilliness").Remove()
            .Match(OpCodes.Ldc_I4_4).Remove()
            .Match(OpCodes.Beq_S).Remove()
            .Match(OpCodes.Ldarg_1).Remove()
            .MatchCall(typeof(Map), "get_TileInfo").Remove()
            .MatchLoad(typeof(Tile), "hilliness").Remove()
            .Match(OpCodes.Ldc_I4_5).Remove()
            .Insert(CodeInstruction.Call(self, nameof(ShouldUseVanillaMountainGeneration)))
            .Match(OpCodes.Bne_Un).StoreOperandIn(brfalseSkip).Remove()
            .Insert(brfalseSkip);

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static bool ShouldUseVanillaMountainGeneration(Tile tile)
    {
        if (tile.hilliness < Hilliness.Mountainous) return false;
        if (!GeologicalLandformsAPI.VanillaMountainGenerationEnabled.Apply(tile)) return false;
        return true;
    }
}

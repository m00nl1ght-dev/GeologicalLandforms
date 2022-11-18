using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using TerrainGraph;
using Verse;
using static TerrainGraph.GridFunction;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

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
        
        elevationModule ??= BuildDefaultElevationGrid(map, elevationSeed);
        fertilityModule ??= BuildDefaultFertilityGrid(map, fertilitySeed);
        
        if (map.TileInfo.WaterCovered) elevationModule = new Min(elevationModule, Of<double>(0f));

        var elevation = MapGenerator.Elevation;
        var fertility = MapGenerator.Fertility;
        
        foreach (var cell in map.AllCells)
        {
            elevation[cell] = (float) elevationModule.ValueAt(cell.x, cell.z);
            fertility[cell] = (float) fertilityModule.ValueAt(cell.x, cell.z);
        }
        
        return false;
    }
    
    /// <summary>
    /// Disables vanilla cliff generation on mountainous tiles.
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.First)]
    private static IEnumerable<CodeInstruction> Generate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var lastOpCode = OpCodes.Nop;
        var patched = false;
    
        foreach (var instruction in instructions)
        {
            if (!patched && lastOpCode == OpCodes.Ldfld && instruction.opcode == OpCodes.Ldc_I4_4)
            {
                patched = true;
                lastOpCode = OpCodes.Ldc_I4_5;
                yield return new CodeInstruction(lastOpCode);
                continue;
            }

            lastOpCode = instruction.opcode;
            yield return instruction;
        }
            
        if (patched == false)
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_GenStep_ElevationFertility");
    }

    private static IGridFunction<double> BuildDefaultElevationGrid(Map map, int seed)
    {
        IGridFunction<double> function = new NoiseGenerator(NodeGridPerlin.PerlinNoise, 0.021, 2, 0.5, 6, seed);
        function = new ScaleWithBias(function, 0.5, 0.5);
        function = new Multiply(function, Of(NodeValueWorldTile.GetHillinessFactor(map.TileInfo.hilliness)));
        if (map.TileInfo.WaterCovered) function = new Min(function, Of<double>(0f));
        return function;
    }
    
    private static IGridFunction<double> BuildDefaultFertilityGrid(Map map, int seed)
    {
        IGridFunction<double> function = new NoiseGenerator(NodeGridPerlin.PerlinNoise, 0.021, 2, 0.5, 6, seed);
        function = new ScaleWithBias(function, 0.5, 0.5);
        return function;
    }
}
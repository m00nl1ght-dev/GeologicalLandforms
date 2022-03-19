using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using TerrainGraph;
using Verse;
using static TerrainGraph.GridFunction;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(GenStep_ElevationFertility), nameof(GenStep_ElevationFertility.Generate))]
internal static class RimWorld_GenStep_ElevationFertility
{
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyBefore("com.configurablemaps.rimworld.mod")]
    private static bool Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (!Landform.IsAnyGenerating) return true;

        IGridFunction<double> elevationModule = Landform.GeneratingLandform.OutputElevation?.Get() ?? BuildDefaultElevationGrid(map);
        IGridFunction<double> fertilityModule = Landform.GeneratingLandform.OutputFertility?.Get() ?? BuildDefaultFertilityGrid(map);
        
        MapGenFloatGrid elevation = MapGenerator.Elevation;
        MapGenFloatGrid fertility = MapGenerator.Fertility;
        
        foreach (IntVec3 cell in map.AllCells)
        {
            elevation[cell] = (float) elevationModule.ValueAt(cell.x, cell.z);
            fertility[cell] = (float) fertilityModule.ValueAt(cell.x, cell.z);
        }
        
        return false;
    }
    
    /// <summary>
    /// Disables vanilla cliff generation on mountainous tiles.
    /// </summary>
    [HarmonyPriority(Priority.First)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        OpCode lastOpCode = OpCodes.Nop;
        var patched = false;
    
        foreach (CodeInstruction instruction in instructions)
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
            Log.Error("Failed to patch RimWorld_GenStep_ElevationFertility");
    }

    public static IGridFunction<double> BuildDefaultElevationGrid(Map map)
    {
        IGridFunction<double> function = new NoiseGenerator(NodeGridPerlin.PerlinNoise, 0.021, 2, 0.5, 6, Rand.Range(0, int.MaxValue));
        function = new ScaleWithBias(function, 0.5, 0.5);
        function = new Multiply(function, Of(NodeValueWorldTile.GetHillinessFactor(map.TileInfo.hilliness)));
        if (map.TileInfo.WaterCovered) function = new Min(function, Of<double>(0f));
        return function;
    }
    
    public static IGridFunction<double> BuildDefaultFertilityGrid(Map map)
    {
        IGridFunction<double> function = new NoiseGenerator(NodeGridPerlin.PerlinNoise, 0.021, 2, 0.5, 6, Rand.Range(0, int.MaxValue));
        function = new ScaleWithBias(function, 0.5, 0.5);
        return function;
    }
}
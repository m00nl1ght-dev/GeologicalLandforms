using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.TerrainGraph;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(GenStep_ElevationFertility), nameof(GenStep_ElevationFertility.Generate))]
internal static class RimWorld_GenStep_ElevationFertility
{
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyBefore("com.configurablemaps.rimworld.mod")]
    private static bool Prefix(Map map, GenStepParams parms)
    {
        // if there is no landform on this tile, let vanilla gen or other mods handle it
        if (Landform.GeneratingLandform == null) return true;

        ModuleBase elevationModule = BuildDefaultElevationModule(map);
        ModuleBase fertilityModule = BuildDefaultFertilityModule(map);
        
        MapGenFloatGrid elevation = MapGenerator.Elevation;
        MapGenFloatGrid fertility = MapGenerator.Fertility;
        
        foreach (IntVec3 cell in map.AllCells)
        {
            elevation[cell] = elevationModule.GetValue(cell);
            fertility[cell] = fertilityModule.GetValue(cell);
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

    public static ModuleBase BuildDefaultElevationModule(Map map)
    {
        ModuleBase module = new ScaleBias(0.5, 0.5, new Perlin(0.021, 2, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High));
        module = new Multiply(module, new Const(GetHillinessFactor(map)));
        if (map.TileInfo.WaterCovered) module = new Min(module, new Const(0f));
        return module;
    }
    
    public static ModuleBase BuildDefaultFertilityModule(Map map)
    {
        return new ScaleBias(0.5, 0.5, new Perlin(0.021, 2, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High));
    }

    public static float GetHillinessFactor(Map map)
    {
        return map.TileInfo.hilliness switch
        {
            Hilliness.Flat => MapGenTuning.ElevationFactorFlat,
            Hilliness.SmallHills => MapGenTuning.ElevationFactorSmallHills,
            Hilliness.LargeHills => MapGenTuning.ElevationFactorLargeHills,
            Hilliness.Mountainous => MapGenTuning.ElevationFactorMountains,
            Hilliness.Impassable => MapGenTuning.ElevationFactorImpassableMountains,
            _ => 1f
        };
    }
}
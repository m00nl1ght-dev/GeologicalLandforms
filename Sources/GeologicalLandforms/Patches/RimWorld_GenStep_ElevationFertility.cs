using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (GenStep_ElevationFertility), nameof(GenStep_ElevationFertility.Generate))]
internal static class RimWorld_GenStep_ElevationFertility
{
    private static WorldTileInfo _worldTileInfo;
    private static GenNoiseConfig _noiseConfig;
    
    private static bool Prefix(Map map, GenStepParams parms)
    {
        _worldTileInfo = WorldTileInfo.GetWorldTileInfo(map.Tile);
        _noiseConfig = _worldTileInfo.Landform?.GenConfig;
        if (_noiseConfig == null) return true;
        
        int mapSizeInt = Math.Min(map.Size.x, map.Size.z);
        if (_worldTileInfo.Landform != null && !_worldTileInfo.Landform.MapSizeRequirement.Includes(mapSizeInt)) return true;
        
        GenNoiseStack noiseStackElevation = _noiseConfig.NoiseStacks.TryGetValue(GenNoiseConfig.NoiseType.Elevation);
        noiseStackElevation ??= new GenNoiseStack(GenNoiseConfig.NoiseType.Elevation);
        
        GenNoiseStack noiseStackFertility = _noiseConfig.NoiseStacks.TryGetValue(GenNoiseConfig.NoiseType.Fertility);
        noiseStackFertility ??= new GenNoiseStack(GenNoiseConfig.NoiseType.Fertility);

        ModuleBase elevationModule = noiseStackElevation.BuildModule(_worldTileInfo, map, "Elevation", QualityMode.High);
        ModuleBase fertilityModule = noiseStackFertility.BuildModule(_worldTileInfo, map, "Fertility", QualityMode.High);

        float hillFactor = 1f + ( GetHillinessFactor(map) - 1f ) * _noiseConfig.HillModifierEffectiveness;
        elevationModule = new Multiply(elevationModule, new Const(hillFactor));

        if (map.TileInfo.WaterCovered) elevationModule = new Min(elevationModule, new Const(_noiseConfig.MaxElevationIfWaterCovered));

        MapGenFloatGrid elevation = MapGenerator.Elevation;
        MapGenFloatGrid fertility = MapGenerator.Fertility;
        foreach (IntVec3 cell in map.AllCells)
        {
            elevation[cell] = elevationModule.GetValue(cell);
            fertility[cell] = fertilityModule.GetValue(cell);
        }
        
        return false;
    }
    
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
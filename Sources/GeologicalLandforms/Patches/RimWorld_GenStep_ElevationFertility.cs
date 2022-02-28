using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (GenStep_ElevationFertility))]
internal static class RimWorld_GenStep_ElevationFertility
{
    private static WorldTileInfo _worldTileInfo;
    private static GenNoiseConfig _noiseConfig;

    [HarmonyPatch(nameof(GenStep_ElevationFertility.Generate))]
    private static bool Prefix(Map map, GenStepParams parms)
    {
        _worldTileInfo = WorldTileInfo.GetWorldTileInfo(map.Tile);
        _noiseConfig = _worldTileInfo.Landform?.GenConfig;
        if (_noiseConfig == null) return true;
        
        Log.Message("Initiating Elevation and Fertility configs for landform " + _worldTileInfo.Topology);

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
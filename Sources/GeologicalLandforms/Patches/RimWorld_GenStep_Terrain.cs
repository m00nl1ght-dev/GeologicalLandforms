using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class RimWorld_GenStep_Terrain
{
    public static NodeOutputTerrain TerrainOutput { get; private set; }
    public static IGridFunction<TerrainData> BaseFunction { get; private set; }
    public static IGridFunction<TerrainData> StoneFunction { get; private set; }
    public static bool DebugWarnedMissingTerrain { get; private set; }

    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(nameof(GenStep_Terrain.Generate))]
    private static void Prefix(Map map, GenStepParams parms)
    {
        Init();
    }
    
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(GenStep_Terrain.Generate))]
    private static void Postfix(Map map, GenStepParams parms)
    {
        CleanUp();
    }
    
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch("TerrainFrom")]
    private static bool Prefix(ref TerrainDef __result, IntVec3 c, Map map, float elevation, float fertility, RiverMaker river, bool preferSolid)
    {
        if (TerrainOutput == null) return true;

        TerrainDef tRiver = river?.TerrainAt(c, true);
        __result = TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }

    public static void Init()
    {
        if (!Landform.IsAnyGenerating) return;
        TerrainOutput = Landform.GeneratingLandform.OutputTerrain;
        BaseFunction = TerrainOutput?.GetBase();
        StoneFunction = TerrainOutput?.GetStone();
    }

    public static void CleanUp()
    {
        TerrainOutput = null;
        BaseFunction = null;
        StoneFunction = null;
    }

    public static TerrainDef TerrainAt(IntVec3 c, Map map, float elevation, float fertility, TerrainDef tRiver, bool preferSolid)
    {
        if (tRiver == null && preferSolid)
        {
            return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
        }

        TerrainDef tBase = BaseFunction?.ValueAt(c.x, c.z).Terrain;
        
        if (tBase.IsDeepWater()) return tBase;
        if (tRiver is { IsRiver: true }) return tRiver;

        if (tBase != null) return tBase;
        if (tRiver != null) return tRiver;

        foreach (TerrainPatchMaker patchMaker in map.Biome.terrainPatchMakers)
        {
            TerrainDef tPatch = patchMaker.TerrainAt(c, map, fertility);
            if (tPatch != null) return tPatch;
        }
        
        TerrainDef tStone = StoneFunction == null ? DefaultElevationTerrain(c, elevation) : StoneFunction.ValueAt(c.x, c.z).Terrain;
        if (tStone != null) return tStone;

        TerrainDef tBiome = TerrainThreshold.TerrainAtValue(map.Biome.terrainsByFertility, fertility);
        if (tBiome != null) return tBiome;

        if (!DebugWarnedMissingTerrain)
        {
            Log.Error("No terrain found in biome " + map.Biome.defName + " for elevation=" + elevation + ", fertility=" + fertility);
            DebugWarnedMissingTerrain = true;
        }
        
        return TerrainDefOf.Sand;
    }

    private static TerrainDef DefaultElevationTerrain(IntVec3 c, float elevation)
    {
        if (elevation < 0.55) return null;
        if (elevation < 0.61) return TerrainDefOf.Gravel;
        return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
    }

    public static bool IsDeepWater(this TerrainDef def)
    {
        return def == TerrainDefOf.WaterDeep || def == TerrainDefOf.WaterOceanDeep;
    }
}
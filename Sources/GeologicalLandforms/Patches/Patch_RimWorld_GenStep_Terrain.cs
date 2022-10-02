using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using LunarFramework.Utility;
using RimWorld;
using TerrainGraph;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class Patch_RimWorld_GenStep_Terrain
{
    public static IGridFunction<TerrainData> BaseFunction { get; private set; }
    public static IGridFunction<TerrainData> StoneFunction { get; private set; }
    public static IGridFunction<BiomeData> BiomeFunction { get; private set; }

    public static bool UseVanillaTerrain { get; private set; } = true;
    public static bool DebugWarnedMissingTerrain { get; private set; }

    private static BiomeGrid _biomeGrid;

    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void Generate_Prefix(Map map, GenStepParams parms)
    {
        Init(map.BiomeGrid());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.Last)]
    private static void Generate_Postfix(Map map, GenStepParams parms)
    {
        CleanUp();
        BiomeTransition.DrawDebug(map.debugDrawer);
        map.BiomeGrid()?.UpdateOpenGroundFraction();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("TerrainFrom")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool TerrainFrom(ref TerrainDef __result, IntVec3 c, Map map, float elevation, float fertility, RiverMaker river, bool preferSolid)
    {
        if (UseVanillaTerrain) return true;

        var tRiver = river?.TerrainAt(c, true);
        __result = TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }

    public static void Init(BiomeGrid biomeGrid = null)
    {
        CleanUp();
        
        var tile = Landform.GeneratingTile as WorldTileInfo;
        var mapSize = Landform.GeneratingMapSize;
        
        if (!Landform.AnyGenerating || tile == null) return;

        BaseFunction = Landform.GetFeature(l => l.OutputTerrain?.GetBase());
        StoneFunction = Landform.GetFeature(l => l.OutputTerrain?.GetStone());
        BiomeFunction = Landform.GetFeature(l => l.OutputBiomeGrid?.GetBiomeGrid());

        _biomeGrid = biomeGrid ?? new BiomeGrid(new IntVec3(mapSize.x, 1, mapSize.z), tile.Biome);
        
        bool hasBiomeTransition = false;
        if (tile.HasBorderingBiomes)
        {
            var transition = Landform.GetFeature(l => l.OutputBiomeGrid?.ApplyBiomeTransitions(tile, mapSize, BiomeFunction));
            if (transition != null)
            {
                BiomeFunction = transition;
                hasBiomeTransition = true;
            }
        }

        if (BiomeFunction != null) _biomeGrid.SetBiomes(BiomeFunction);
        if (hasBiomeTransition)
        {
            BiomeTransition.PostProcessBiomeGrid(_biomeGrid, tile, mapSize);
        }
        
        GeologicalLandformsAPI.RunApplyBiomeReplacements(tile, _biomeGrid);

        UseVanillaTerrain = BaseFunction == null && StoneFunction == null && BiomeFunction == null;
    }

    public static void CleanUp()
    {
        UseVanillaTerrain = true;
        BaseFunction = null;
        StoneFunction = null;
        BiomeFunction = null;
        _biomeGrid = null;
    }

    public static TerrainDef TerrainAt(IntVec3 c, Map map, float elevation, float fertility, TerrainDef tRiver, bool preferSolid)
    {
        if (tRiver == null && preferSolid)
        {
            return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
        }

        var tBase = BaseFunction?.ValueAt(c.x, c.z).Terrain;
        
        if (tBase.IsDeepWater()) return tBase;
        if (tRiver is { IsRiver: true }) return tRiver;

        if (tBase != null) return tBase;
        if (tRiver != null) return tRiver;

        var biome = _biomeGrid?.BiomeAt(c) ?? Landform.GeneratingTile?.Biome ?? map.Biome;
        foreach (var patchMaker in biome.terrainPatchMakers)
        {
            var tPatch = patchMaker.TerrainAt(c, map, fertility);
            if (tPatch != null) return tPatch;
        }
        
        var tStone = StoneFunction == null ? DefaultElevationTerrain(c, elevation) : StoneFunction.ValueAt(c.x, c.z).Terrain;
        if (tStone != null) return tStone;

        var tBiome = TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, fertility);
        if (tBiome != null) return tBiome;

        if (!DebugWarnedMissingTerrain)
        {
            Log.Error("No terrain found in biome " + biome.defName + " for elevation=" + elevation + ", fertility=" + fertility);
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
}
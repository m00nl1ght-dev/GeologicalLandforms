using System;
using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using Verse;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class RimWorld_GenStep_Terrain
{
    [TweakValue("GLF")]
    public static float ApplyBGPost = 1f;
    
    public static IGridFunction<TerrainData> BaseFunction { get; private set; }
    public static IGridFunction<TerrainData> StoneFunction { get; private set; }
    public static IGridFunction<BiomeData> BiomeFunction { get; private set; }

    public static bool UseVanillaTerrain { get; private set; } = true;
    public static bool DebugWarnedMissingTerrain { get; private set; }

    private static BiomeGrid _biomeGrid;

    private static List<IntVec3> _tpmProcessed = new();

    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(nameof(GenStep_Terrain.Generate))]
    private static void Prefix(Map map, GenStepParams parms)
    {
        var biomeGrid = map.GetComponent<BiomeGrid>();
        Init(biomeGrid);
    }
    
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(GenStep_Terrain.Generate))]
    private static void Postfix(Map map, GenStepParams parms)
    {
        CleanUp();
        foreach (var intVec3 in _tpmProcessed)
        {
            map.debugDrawer.FlashCell(intVec3, 0f, null, 1000);
        }
    }
    
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch("TerrainFrom")]
    private static bool Prefix(ref TerrainDef __result, IntVec3 c, Map map, float elevation, float fertility, RiverMaker river, bool preferSolid)
    {
        if (UseVanillaTerrain) return true;

        var tRiver = river?.TerrainAt(c, true);
        __result = TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }

    public static void Init(BiomeGrid biomeGrid = null)
    {
        CleanUp();
        
        if (!Landform.AnyGenerating) return;
        
        BaseFunction = Landform.GetFeature(l => l.OutputTerrain?.GetBase());
        StoneFunction = Landform.GetFeature(l => l.OutputTerrain?.GetStone());
        BiomeFunction = Landform.GetFeature(l => l.OutputBiomeGrid?.GetBiomeGrid());
        
        var tile = Landform.GeneratingTile;
        var mapSize = Landform.GeneratingMapSize;
        _biomeGrid = biomeGrid;
        
        if (tile?.BorderingBiomes?.Count > 0 && tile is WorldTileInfo tileInfo)
        {
            var transition = Landform.GetFeature(l => l.OutputBiomeGrid?.ApplyBiomeTransitions(tile, mapSize, BiomeFunction));
            if (transition != null)
            {
                BiomeFunction = transition;
                _biomeGrid ??= new BiomeGrid(new IntVec3(mapSize, 0, mapSize), tile.Biome);
                PreprocessBiomeTransitions(tileInfo, mapSize);
            }
        }
        
        UseVanillaTerrain = BaseFunction == null && StoneFunction == null && BiomeFunction == null;
    }

    private static void PreprocessBiomeTransitions(WorldTileInfo tile, int mapSize)
    {
        var tempMap = CreateMinimalMap(tile.TileId, mapSize);
        var floodFiller = new FloodFiller(tempMap);

        for (int x = 0; x < mapSize; x++) for (int z = 0; z < mapSize; z++)
        {
            var c = new IntVec3(x, 0, z);
            var cBiome = BiomeFunction.ValueAt(c.x, c.z).Biome ?? tile.Biome;
            _biomeGrid.SetBiome(c, cBiome);
        }
        
        List<IntVec3> processed = new();
        for (int x = 0; x < mapSize; x++) for (int z = 0; z < mapSize; z++)
        {
            var c = new IntVec3(x, 0, z);
            var cBiome = _biomeGrid.BiomeAt(c);

            TerrainDef TpmOutput(IntVec3 i, BiomeDef b)
            {
                foreach (var patchMaker in b.terrainPatchMakers)
                {
                    var t = patchMaker.TerrainAt(i, tempMap, patchMaker.minFertility + 0.5f);
                    if (t != null) return t;
                }
                return null;
            }

            var cTpm = TpmOutput(c, cBiome);
            if (cTpm == null) continue;
            
            c.ForEachAdjacent(mapSize, a =>
            {
                var aBiome = _biomeGrid.BiomeAt(a);
                if (aBiome != cBiome)
                {
                    floodFiller.FloodFill(a, p =>
                    {
                        var pBiome = _biomeGrid.BiomeAt(p);
                        if (pBiome == cBiome) return false;
                        var cpTpm = TpmOutput(p, cBiome);
                        if (cpTpm == null) return false;
                        var ppTpm = TpmOutput(p, pBiome);
                        return ppTpm == null || ShouldOverride(cpTpm, ppTpm);
                    }, p =>
                    {
                        _biomeGrid.SetBiome(p, cBiome);
                        processed.Add(p);
                    });
                }
            });
        }
        
        Log.Message("TPM preprocessor changed biome of " + processed.Count + " tiles.");
        _tpmProcessed = processed;
        
        if (ApplyBGPost < 1f) for (int x = 0; x < mapSize; x++) for (int z = 0; z < mapSize; z++)
        {
            var c = new IntVec3(x, 0, z);
            var cBiome = BiomeFunction.ValueAt(c.x, c.z).Biome ?? tile.Biome;
            _biomeGrid.SetBiome(c, cBiome);
        }
    }

    private static bool ShouldOverride(TerrainDef terrain, TerrainDef other)
    {
        return terrain.IsWater && !other.IsWater;
        //if (terrain.passability > other.passability) return true;
        //if (terrain.passability < other.passability) return false;
        //if (terrain.pathCost > other.pathCost) return true;
        //if (terrain.pathCost < other.pathCost) return false;
        //return true;
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

    public static bool IsDeepWater(this TerrainDef def)
    {
        return def == TerrainDefOf.WaterDeep || def == TerrainDefOf.WaterOceanDeep;
    }

    private static void ForEachAdjacent(this IntVec3 c, int mapSize, Action<IntVec3> action)
    {
        if (c.x > 0) action(new IntVec3(c.x - 1, c.y, c.z));
        if (c.z > 0) action(new IntVec3(c.x, c.y, c.z - 1));
        if (c.x < mapSize - 1) action(new IntVec3(c.x + 1, c.y, c.z));
        if (c.z < mapSize - 1) action(new IntVec3(c.x, c.y, c.z + 1));
    }
    
    private static Map CreateMinimalMap(int tile, int mapSize)
    {
        var map = new Map { info = { parent = new MapParent {Tile = tile}, Size = new IntVec3(mapSize, 1, mapSize) } };
        map.cellIndices = new CellIndices(map);
        map.floodFiller = new FloodFiller(map);
        return map;
    }
}
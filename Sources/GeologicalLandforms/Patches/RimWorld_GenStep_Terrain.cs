using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class RimWorld_GenStep_Terrain
{
    [TweakValue("Geological Landforms", 0.0f, 0.5f)]
    private static float BorderingBiomeNoiseFrequency = 0.01f; // 0.21f;
    [TweakValue("Geological Landforms", 0.0f, 5f)]
    private static float BorderingBiomeNoiseLacunarity = 2f;
    [TweakValue("Geological Landforms", 0.0f, 2f)]
    private static float BorderingBiomeNoisePersistence = 0.5f;
    [TweakValue("Geological Landforms", -2f, 2f)]
    private static float BorderingBiomeBias = 0.325f; // 0.85f;
    [TweakValue("Geological Landforms", 0f, 300f)]
    private static float BorderingBiomeSpan = 100f; // 50f;
    
    public static IGridFunction<TerrainData> BaseFunction { get; private set; }
    public static IGridFunction<TerrainData> StoneFunction { get; private set; }
    public static IGridFunction<BiomeData> BiomeFunction { get; private set; }

    public static bool UseVanillaTerrain { get; private set; } = true;
    public static bool DebugWarnedMissingTerrain { get; private set; }

    private static BiomeGrid _biomeGrid;

    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(nameof(GenStep_Terrain.Generate))]
    private static void Prefix(Map map, GenStepParams parms)
    {
        var biomeGrid = map.GetComponent<BiomeGrid>();
        biomeGrid?.Init(map.TileInfo.biome);
        Init(map.Tile, biomeGrid);
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
        if (UseVanillaTerrain) return true;

        TerrainDef tRiver = river?.TerrainAt(c, true);
        __result = TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }

    public static void Init(int tileId, BiomeGrid biomeGrid = null)
    {
        CleanUp();
        
        if (Landform.IsAnyGenerating)
        {
            NodeOutputTerrain terrainOutput = Landform.GeneratingLandform.OutputTerrain;
            NodeOutputBiomeGrid biomeOutput = Landform.GeneratingLandform.OutputBiomeGrid;
            BaseFunction = terrainOutput?.GetBase();
            StoneFunction = terrainOutput?.GetStone();
            BiomeFunction = biomeOutput?.GetBiomeGrid();
        }

        if (Landform.GeneratingTile?.BorderingBiomes?.Count > 0)
        {
            BiomeFunction = new BiomeBorderGen(BiomeFunction, Landform.GeneratingTile, tileId, Landform.GeneratingMapSize);
        }
        
        UseVanillaTerrain = BaseFunction == null && StoneFunction == null && BiomeFunction == null;
        _biomeGrid = biomeGrid;
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
        BiomeDef biome = BiomeFunction?.ValueAt(c.x, c.z).Biome ?? map.Biome;
        _biomeGrid?.SetBiome(c, biome);
        
        if (tRiver == null && preferSolid)
        {
            return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
        }

        TerrainDef tBase = BaseFunction?.ValueAt(c.x, c.z).Terrain;
        
        if (tBase.IsDeepWater()) return tBase;
        if (tRiver is { IsRiver: true }) return tRiver;

        if (tBase != null) return tBase;
        if (tRiver != null) return tRiver;

        foreach (TerrainPatchMaker patchMaker in biome.terrainPatchMakers)
        {
            TerrainDef tPatch = patchMaker.TerrainAt(c, map, fertility);
            if (tPatch != null) return tPatch;
        }
        
        TerrainDef tStone = StoneFunction == null ? DefaultElevationTerrain(c, elevation) : StoneFunction.ValueAt(c.x, c.z).Terrain;
        if (tStone != null) return tStone;

        TerrainDef tBiome = TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, fertility);
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

    private class BiomeBorderGen : IGridFunction<BiomeData>
    {
        private readonly IGridFunction<BiomeData> _preFunc;
        private readonly BiomeDef _primary;
        
        private IGridFunction<double>[] _selFuncs;
        private BiomeDef[] _biomes;

        public BiomeBorderGen(IGridFunction<BiomeData> preFunc, IWorldTileInfo tile, int tileId, int mapSize)
        {
            _preFunc = preFunc;
            _primary = tile.Biome;
            InitSelFuncs(tile, tileId, mapSize);
        }

        private void InitSelFuncs(IWorldTileInfo tile, int tileId, int mapSize)
        {
            _biomes = new BiomeDef[tile.BorderingBiomes.Count];
            _selFuncs = new IGridFunction<double>[_biomes.Length];
            
            for (var i = 0; i < _biomes.Length; i++)
            {
                IWorldTileInfo.BorderingBiome borderingBiome = tile.BorderingBiomes[i];
                _biomes[i] = borderingBiome.Biome;

                int seed = Find.World.info.Seed ^ tileId ^ 1753 ^ i;
                IGridFunction<double> func = new GridFunction.NoiseGenerator(
                    NodeGridPerlin.PerlinNoise, 
                    BorderingBiomeNoiseFrequency, 
                    BorderingBiomeNoiseLacunarity, 
                    BorderingBiomeNoisePersistence, 
                    6, seed);
                func = new GridFunction.Add(func, new GridFunction.SpanFunction(BorderingBiomeBias, 0, 0, -BorderingBiomeSpan, 0, 0, 0, false));
                func = new GridFunction.Rotate<double>(func, mapSize / 2f, mapSize / 2f, borderingBiome.Angle + 90f);
                _selFuncs[i] = func;
            }
        }

        public BiomeData ValueAt(double x, double z)
        {
            BiomeDef pre = _preFunc?.ValueAt(x, z).Biome;
            if (pre != null) return new BiomeData(pre);

            double v = 0;
            BiomeDef b = _primary;
            for (var i = 0; i < _biomes.Length; i++)
            {
                var sel = _selFuncs[i].ValueAt(x, z);
                if (sel > v)
                {
                    v = sel;
                    b = _biomes[i];
                }
            }

            return new BiomeData(b);
        }
    }
}
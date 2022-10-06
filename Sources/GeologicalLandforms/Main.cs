using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using NodeEditorFramework.Utilities;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class Main
{
    public static readonly IReadOnlyCollection<string> ExcludedBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands", // Biomes! Islands
        "BMT_FungalForest", // Biomes! Caverns
        "BMT_ChromaticOasis", // Biomes! Oasis
        "Tunnelworld", "InfestedMountains", "DeepRavine", "FrozenLake", "Oasis", // Terra Project
        "Archipelago", "VolcanicIsland", "TundraSkerries", "PackIce", "Atoll", // Terra Project
        "Cave" // CaveBiome, Terra Project
    };
    
    public static readonly IReadOnlyCollection<string> OceanTopologyBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands", // Biomes! Islands
        "Archipelago", "VolcanicIsland", "TundraSkerries", "Atoll" // Terra Project
    };

    private static readonly HashSet<BiomeDef> ExcludedBiomes = new();
    private static readonly HashSet<BiomeDef> OceanTopologyBiomes = new();

    static Main()
    {
        new Harmony("Geological Landforms").PatchAll();
        
        ParseHelper.Parsers<LandformData.Entry>.Register(GeologicalLandforms.LandformData.Entry.FromString);
        
        ExcludedBiomes.AddRange(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => ExcludedBiomePrefixes.Any(b.defName.StartsWith)));
        OceanTopologyBiomes.AddRange(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => OceanTopologyBiomePrefixes.Any(b.defName.StartsWith)));
        
        ReflectionUtility.AddSearchableAssembly(typeof(Main).Assembly);
        ReflectionUtility.AddSearchableAssembly(typeof(TerrainCanvas).Assembly);
        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
    }

    public static Rot6 Random(this List<Rot6> rotList, int seed)
    {
        if (rotList.Count == 0) return Rot6.Invalid;
        return rotList[Rand.RangeSeeded(0, rotList.Count, seed)];
    }

    public static bool IsBiomeExcluded(BiomeDef biome)
    {
        return ExcludedBiomes.Contains(biome);
    }
    
    public static bool IsBiomeOceanTopology(BiomeDef biome)
    {
        return OceanTopologyBiomes.Contains(biome);
    }
    
    public static bool IsVanillaBodyOfWater(BiomeDef biome)
    {
        return biome == BiomeDefOf.Ocean || biome == BiomeDefOf.Lake;
    }

    private static LandformData _landformDataCache;
    
    public static LandformData LandformData(this World world)
    {
        if (world == null) return null;
        if (_landformDataCache?.world == world) return _landformDataCache;
        _landformDataCache = world.GetComponent<LandformData>();
        return _landformDataCache;
    }
    
    private static BiomeGrid _biomeGridCache;
    
    public static BiomeGrid BiomeGrid(this Map map)
    {
        if (map == null) return null;
        if (_biomeGridCache?.map == map) return _biomeGridCache;
        _biomeGridCache = map.GetComponent<BiomeGrid>();
        return _biomeGridCache;
    }
}
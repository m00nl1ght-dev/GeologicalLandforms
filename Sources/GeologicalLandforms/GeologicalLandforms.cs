using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class GeologicalLandforms
{
    public static Version LibVersion => typeof(GeologicalLandforms).Assembly.GetName().Version;

    public static string LogPrefix => "[Geological Landforms v" + LibVersion + "] ";

    static GeologicalLandforms()
    {
        new Harmony("Geological Landforms").PatchAll();
        
        ReflectionUtility.AddSearchableAssembly(typeof(GeologicalLandforms).Assembly);
        ReflectionUtility.AddSearchableAssembly(typeof(TerrainCanvas).Assembly);
        
        NodeEditor.ReInit(false);
        
        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
    }
    
    private static readonly IReadOnlyCollection<string> ExcludedBiomePrefixes = new HashSet<string>
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

    private static HashSet<BiomeDef> _excludedBiomes;
    private static HashSet<BiomeDef> _oceanTopologyBiomes;
    
    public static bool IsBiomeExcluded(BiomeDef biome)
    {
        _excludedBiomes ??= new(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => ExcludedBiomePrefixes.Any(b.defName.StartsWith)));
        return _excludedBiomes.Contains(biome);
    }
    
    public static bool IsBiomeOceanTopology(BiomeDef biome)
    {
        _oceanTopologyBiomes ??= new(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => OceanTopologyBiomePrefixes.Any(b.defName.StartsWith)));
        return _oceanTopologyBiomes.Contains(biome);
    }

    public static Func<BiomeGrid, float> AnimalDensityFactorFunc { get; set; } = _ => 1f;
}
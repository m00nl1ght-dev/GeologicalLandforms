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
public static class Main
{
    public static Version LibVersion => typeof(Main).Assembly.GetName().Version;

    public static string LogPrefix => "[Geological Landforms v" + LibVersion + "] ";

    static Main()
    {
        new Harmony("Geological Landforms").PatchAll();
        
        ReflectionUtility.AddSearchableAssembly(typeof(Main).Assembly);
        ReflectionUtility.AddSearchableAssembly(typeof(TerrainCanvas).Assembly);
        
        NodeEditor.ReInit(false);
        
        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
    }
    
    private static readonly IReadOnlyCollection<string> ExcludedBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands", // Biomes! Islands // TODO make islands be considered ocean tiles in topology calc
        "BMT_FungalForest", // Biomes! Caverns
        "BMT_ChromaticOasis", // Biomes! Oasis
        "Tunnelworld", "InfestedMountains", "DeepRavine", "FrozenLake", "Oasis", // Terra Project
        "Archipelago", "VolcanicIsland", "TundraSkerries", "PackIce", "Atoll", // Terra Project
        "Cave" // CaveBiome, Terra Project
    };

    private static HashSet<BiomeDef> _excludedBiomes;
    
    public static bool IsBiomeExcluded(BiomeDef biome)
    {
        _excludedBiomes ??= new(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => ExcludedBiomePrefixes.Any(b.defName.StartsWith)));
        return _excludedBiomes.Contains(biome);
    }
}
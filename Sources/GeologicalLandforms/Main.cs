using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using NodeEditorFramework.Utilities;
using RimWorld;
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

    static Main()
    {
        new Harmony("Geological Landforms").PatchAll();
        
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
        return ExcludedBiomePrefixes.Any(biome.defName.StartsWith);
    }
}
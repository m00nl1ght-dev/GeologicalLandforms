using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeologicalLandforms;

public static class BiomeUtils
{
    private static readonly IReadOnlyCollection<string> ExcludedBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands", // Biomes! Islands
        "BMT_FungalForest", "BMT_ShallowCave", "BMT_CrystalCaverns", // Biomes! Caverns
        "BMT_ChromaticOasis", // Biomes! Oasis
        "Tunnelworld", "InfestedMountains", "DeepRavine", "FrozenLake", "Oasis", // Terra Project
        "Archipelago", "VolcanicIsland", "TundraSkerries", "PackIce", "Atoll", // Terra Project
        "Cave" // CaveBiome, Terra Project
    };
    
    private static readonly IReadOnlyCollection<string> OceanTopologyBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands", // Biomes! Islands
        "Archipelago", "VolcanicIsland", "TundraSkerries", "Atoll" // Terra Project
    };

    private static HashSet<BiomeDef> _excludedBiomes;
    private static HashSet<BiomeDef> _oceanTopologyBiomes;
    
    public static bool IsExcluded(this BiomeDef biome)
    {
        _excludedBiomes ??= new(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => ExcludedBiomePrefixes.Any(b.defName.StartsWith)));
        return _excludedBiomes.Contains(biome);
    }
    
    public static bool IsOceanTopology(this BiomeDef biome)
    {
        _oceanTopologyBiomes ??= new(DefDatabase<BiomeDef>.AllDefsListForReading.Where(b => OceanTopologyBiomePrefixes.Any(b.defName.StartsWith)));
        return _oceanTopologyBiomes.Contains(biome);
    }
    
    public static bool IsVanillaBodyOfWater(this BiomeDef biome)
    {
        return biome == BiomeDefOf.Ocean || biome == BiomeDefOf.Lake;
    }
}
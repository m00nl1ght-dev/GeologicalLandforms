using System.Collections.Generic;
using RimWorld;
using Verse;
using static GeologicalLandforms.BiomeGrid.BiomeQuery;

// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.ModCompat;

[StaticConstructorOnStartup]
internal static class ModCompat_BiomesIslands
{
    public static readonly bool IsActive;

    private static readonly Dictionary<string, string> BiomeMapping = new()
    {
        {"BorealForest", "BiomesIslands_BorealIsland"},
        {"Tundra", "BiomesIslands_TundraIsland"},
        {"ColdBog", "BiomesIslands_BorealIsland"},
        {"TemperateForest", "BiomesIslands_TemperateIsland"},
        {"TemperateSwamp", "BiomesIslands_TemperateIsland"},
        {"TropicalRainforest", "BiomesIslands_TropicalIsland"},
        {"TropicalSwamp", "BiomesIslands_TropicalIsland"},
        {"AridShrubland", "BiomesIslands_DesertIsland"},
        {"Desert", "BiomesIslands_DesertIsland"},
        {"ExtremeDesert", "BiomesIslands_DesertIsland"}
    };

    static ModCompat_BiomesIslands()
    {
        try
        {
            var biType = GenTypes.GetTypeInAnyAssembly("BiomesIslands.BiomesIslands");
            if (biType != null)
            {
                Log.Message(Main.LogPrefix + "Applying integration with Biomes! Islands.");

                EventHooks.ApplyBiomeReplacements += ApplyBiomeReplacements;
                
                IsActive = true;
            }
        }
        catch
        {
            Log.Error(Main.LogPrefix + "Failed to apply integration with Biomes! Islands!");
        }
    }

    private static void ApplyBiomeReplacements(WorldTileInfo tile, BiomeGrid biomeGrid)
    {
        if (!ModInstance.Settings.ModCompat_BiomesIslands_CoastPlants &&
            !ModInstance.Settings.ModCompat_BiomesIslands_CoastAnimals) return;
        
        if (!tile.Coast.Any(c => c != IWorldTileInfo.CoastType.None)) return;

        var replacements = new Dictionary<BiomeDef, BiomeDef>();
        foreach (var biomeDef in biomeGrid.CellCounts.Keys)
        {
            if (BiomeMapping.TryGetValue(biomeDef.defName, out var islandName))
            {
                var islandDef = DefDatabase<BiomeDef>.GetNamedSilentFail(islandName);
                if (islandDef != null) replacements[biomeDef] = islandDef;
            }
        }

        if (replacements.Count > 0)
        {
            var biomeQueries = new List<BiomeGrid.BiomeQuery>();
            if (ModInstance.Settings.ModCompat_BiomesIslands_CoastPlants) biomeQueries.Add(PlantSpawning);
            if (ModInstance.Settings.ModCompat_BiomesIslands_CoastAnimals) biomeQueries.Add(AnimalSpawning);
            biomeGrid.SetReplacements(replacements, biomeQueries);
        }
    }
}
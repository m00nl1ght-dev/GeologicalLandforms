using System.Collections.Generic;
using LunarFramework.Patching;
using RimWorld;
using Verse;
using static GeologicalLandforms.BiomeGrid.BiomeQuery;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Compatibility;

internal class ModCompat_BiomesIslands : ModCompat
{
    public override string TargetAssemblyName => "BiomesIslands";
    public override string DisplayName => "Biomes! Islands";
    
    public static bool IsApplied { get; private set; }

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

    protected override bool OnApply()
    {
        GeologicalLandformsAPI.ApplyBiomeReplacements += ApplyBiomeReplacements;
        IsApplied = true;
        return true;
    }

    private static void ApplyBiomeReplacements(WorldTileInfo tile, BiomeGrid biomeGrid)
    {
        if (!GeologicalLandformsMod.Settings.ModCompat_BiomesIslands_CoastPlants &&
            !GeologicalLandformsMod.Settings.ModCompat_BiomesIslands_CoastAnimals) return;
        
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
            if (GeologicalLandformsMod.Settings.ModCompat_BiomesIslands_CoastPlants) biomeQueries.Add(PlantSpawning);
            if (GeologicalLandformsMod.Settings.ModCompat_BiomesIslands_CoastAnimals) biomeQueries.Add(AnimalSpawning);
            biomeGrid.SetReplacements(replacements, biomeQueries);
        }
    }
}
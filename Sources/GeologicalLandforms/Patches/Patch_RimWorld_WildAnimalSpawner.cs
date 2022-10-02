using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WildAnimalSpawner))]
internal static class Patch_RimWorld_WildAnimalSpawner
{
    [HarmonyPrefix]
    [HarmonyPatch("DesiredAnimalDensity", MethodType.Getter)]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool DesiredAnimalDensity(Map ___map, ref float __result)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid == null) return true;

        float total = 0f;
        if (biomeGrid.ShouldApplyForAnimalSpawning)
        {
            float cells = ___map.cellIndices.NumGridCells;
            foreach (var pair in biomeGrid.CellCounts)
            {
                var val = RawDesiredAnimalDensityForBiome(___map, pair.Key);
                total += val * (pair.Value / cells);
            }
        }
        else
        {
            total = RawDesiredAnimalDensityForBiome(___map, ___map.TileInfo.biome);
        }

        __result = total * GeologicalLandformsAPI.AnimalDensityFactorFunction(biomeGrid) * AggregateAnimalDensityFactor(___map.gameConditionManager, ___map);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool SpawnRandomWildAnimalAt(Map ___map, ref bool __result, IntVec3 loc)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForAnimalSpawning: true }) return true;

        var biome = biomeGrid.BiomeAt(loc, BiomeGrid.BiomeQuery.AnimalSpawning);

        var kindDef = biome.AllWildAnimals
            .Where(a => ___map.mapTemperature.SeasonAcceptableFor(a.race))
            .RandomElementByWeight(def => biome.CommonalityOfAnimal(def) / def.wildGroupSize.Average);
        
        if (kindDef == null)
        {
            Log.Error("No spawnable animals right now.");
            __result = false;
            return false;
        }
        
        int randomInRange = kindDef.wildGroupSize.RandomInRange;
        int radius = Mathf.CeilToInt(Mathf.Sqrt(kindDef.wildGroupSize.max));
        for (int index = 0; index < randomInRange; ++index)
        {
            var loc1 = CellFinder.RandomClosewalkCellNear(loc, ___map, radius);
            GenSpawn.Spawn(PawnGenerator.GeneratePawn(kindDef), loc1, ___map);
        }

        __result = true;
        return false;
    }
    
    private static float RawDesiredAnimalDensityForBiome(Map map, BiomeDef biome)
    {
        float animalDensity = biome.animalDensity;
        float num1 = 0.0f;
        float num2 = 0.0f;
        
        foreach (var allWildAnimal in biome.AllWildAnimals)
        {
            float num3 = biome.CommonalityOfAnimal(allWildAnimal);
            num2 += num3;
            if (map.mapTemperature.SeasonAcceptableFor(allWildAnimal.race))
                num1 += num3;
        }

        return animalDensity * (num1 / num2);
    }
    
    private static float AggregateAnimalDensityFactor(GameConditionManager manager, Map map)
    {
        float num = 1f;
        foreach (var t in manager.ActiveConditions)
            num *= t.AnimalDensityFactor(map);
        if (manager.Parent != null)
            num *= AggregateAnimalDensityFactor(manager.Parent, map);
        return num;
    }
    
}
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

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
        if (biomeGrid.Enabled)
        {
            float cells = ___map.cellIndices.NumGridCells;
            foreach (var entry in biomeGrid.Entries)
            {
                var val = RawDesiredAnimalDensityForBiome(___map, entry.Biome);
                total += val * (entry.CellCount / cells);
            }
        }
        else
        {
            total = RawDesiredAnimalDensityForBiome(___map, ___map.TileInfo.biome);
        }

        __result = total * GeologicalLandformsAPI.AnimalDensityFactorFunction(biomeGrid) * AggregateAnimalDensityFactor(___map.gameConditionManager, ___map);

        if (ModsConfig.BiotechActive)
        {
            __result *= PollutionToAnimalDensityFactorCurve.Evaluate(Find.WorldGrid[___map.Tile].pollution);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool SpawnRandomWildAnimalAt(Map ___map, ref bool __result, IntVec3 loc)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        var biome = biomeGrid.BiomeAt(loc);

        var kindDef = biome.AllWildAnimals
            .Where(a => ___map.mapTemperature.SeasonAcceptableFor(a.race))
            .RandomElementByWeight(def => CommonalityOfAnimalNow(def, biome, ___map.TileInfo.pollution));

        if (kindDef == null)
        {
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

    private static float CommonalityOfAnimalNow(PawnKindDef def, BiomeDef biome, float pollution) =>
        (!ModsConfig.BiotechActive || Rand.Value >= PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(pollution)
            ? biome.CommonalityOfAnimal(def) : biome.CommonalityOfPollutionAnimal(def)) / def.wildGroupSize.Average;

    private static readonly SimpleCurve PollutionToAnimalDensityFactorCurve = new() { new(0.1f, 1f), new(1f, 0.25f) };
    private static readonly SimpleCurve PollutionAnimalSpawnChanceFromPollutionCurve = new() { new(0.0f, 0.0f), new(0.25f, 0.1f), new(0.75f, 0.9f), new(1f, 1f) };
}

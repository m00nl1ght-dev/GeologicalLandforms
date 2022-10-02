using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WildPlantSpawner))]
internal static class Patch_RimWorld_WildPlantSpawner
{
    // ### Thread static caches for good ol RimThreaded ###
    
    [ThreadStatic] 
    private static List<ThingDef> _tsc_possiblePlants;
    
    [ThreadStatic] 
    private static List<ThingDef> _tsc_plantDefsLowerOrder;
    
    [ThreadStatic] 
    private static Dictionary<ThingDef, List<float>> _tsc_nearbyClusters;
    
    [ThreadStatic] 
    private static List<KeyValuePair<ThingDef, List<float>>> _tsc_nearbyClustersList;

    [ThreadStatic] 
    private static Dictionary<ThingDef, float> _tsc_distanceSqToNearbyClusters;

    [ThreadStatic] 
    private static List<KeyValuePair<ThingDef, float>> _tsc_possiblePlantsWithWeight;
    
    // ### Patches ###
    
    [HarmonyPrefix]
    [HarmonyPatch("CurrentWholeMapNumDesiredPlants", MethodType.Getter)]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool CurrentWholeMapNumDesiredPlants(WildPlantSpawner __instance, Map ___map, ref float __result)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return true;

        var condFactor = AggregatePlantDensityFactor(___map.gameConditionManager, ___map);

        var cellRect = CellRect.WholeMap(___map);
        
        float numDesiredPlants = 0.0f;
        foreach (var intVec3 in cellRect)
            numDesiredPlants += __instance.GetDesiredPlantsCountAt(intVec3, intVec3, 
                biomeGrid.BiomeAt(intVec3, BiomeGrid.BiomeQuery.PlantSpawning).plantDensity * condFactor);
            
        __result = numDesiredPlants;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("WildPlantSpawnerTickInternal")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool WildPlantSpawnerTickInternal(
        WildPlantSpawner __instance, Map ___map,
        ref float ___calculatedWholeMapNumDesiredPlants,
        ref float ___calculatedWholeMapNumDesiredPlantsTmp,
        ref int ___calculatedWholeMapNumNonZeroFertilityCells,
        ref int ___calculatedWholeMapNumNonZeroFertilityCellsTmp,
        ref int ___cycleIndex)
    {
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return true;
        
        int area = ___map.Area;
        int num = Mathf.CeilToInt(area * 0.0001f);
        float chanceFromDensity = __instance.CachedChanceFromDensity;
        float condFactor = AggregatePlantDensityFactor(___map.gameConditionManager, ___map);
        int checkDuration = Mathf.CeilToInt(10000f);

        for (int index = 0; index < num; ++index)
        {
            if (___cycleIndex >= area)
            {
                ___calculatedWholeMapNumDesiredPlants = ___calculatedWholeMapNumDesiredPlantsTmp;
                ___calculatedWholeMapNumDesiredPlantsTmp = 0.0f;
                ___calculatedWholeMapNumNonZeroFertilityCells = ___calculatedWholeMapNumNonZeroFertilityCellsTmp;
                ___calculatedWholeMapNumNonZeroFertilityCellsTmp = 0;
                ___cycleIndex = 0;
            }
            
            var intVec3 = ___map.cellsInRandomOrder.Get(___cycleIndex);
            var biome = biomeGrid.BiomeAt(intVec3, BiomeGrid.BiomeQuery.PlantSpawning);
            float plantDensity = biome.plantDensity * condFactor;
            
            ___calculatedWholeMapNumDesiredPlantsTmp += __instance.GetDesiredPlantsCountAt(intVec3, intVec3, plantDensity);
            
            if (intVec3.GetTerrain(___map).fertility > 0.0)
                ++___calculatedWholeMapNumNonZeroFertilityCellsTmp;

            bool cavePlant = GoodRoofForCavePlant(___map, intVec3);
            float mtb = cavePlant ? 130f : biome.wildPlantRegrowDays;
            if (Rand.Chance(chanceFromDensity) && Rand.MTBEventOccurs(mtb, 60000f, checkDuration) && CanRegrowAt(___map, intVec3))
            {
                if (cavePlant)
                    __instance.CheckSpawnWildPlantAt(intVec3, plantDensity, ___calculatedWholeMapNumDesiredPlants);
                else
                    CheckSpawnWildPlantAt_Patched_NonCave(__instance, biome, ___map, intVec3, plantDensity, ___calculatedWholeMapNumDesiredPlants);
            }

            ++___cycleIndex;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GenStep_Plants), "Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool Generate(Map map)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return true;
        
        float condFactor = AggregatePlantDensityFactor(map.gameConditionManager, map);
        float desired = map.wildPlantSpawner.CurrentWholeMapNumDesiredPlants;
        
        foreach (var c in map.cellsInRandomOrder.GetAll())
        {
            if (!Rand.Chance(1f / 1000f))
            {
                var biome = biomeGrid.BiomeAt(c, BiomeGrid.BiomeQuery.PlantSpawning);
                float density = biome.plantDensity * condFactor;

                if (GoodRoofForCavePlant(map, c))
                    map.wildPlantSpawner.CheckSpawnWildPlantAt(c, density, desired, true);
                else
                    CheckSpawnWildPlantAt_Patched_NonCave(map.wildPlantSpawner, biome, map, c, density, desired, true);
            }
        }
        
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Command_SetPlantToGrow), "IsPlantAvailable")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool IsPlantAvailable(ThingDef plantDef, Map map, ref bool __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return true;

        if (!plantDef.plant.mustBeWildToSow) return true;

        var researchPrerequisites = plantDef.plant.sowResearchPrerequisites;
        if (researchPrerequisites != null && Enumerable.Any(researchPrerequisites, project => !project.IsFinished))
            return true;

        __result = biomeGrid.CellCounts.Keys.SelectMany(b => b.AllWildPlants).Contains(plantDef);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnimalPenManager), "GetFixedAutoCutFilter")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void GetFixedAutoCutFilter(Map ___map, ref ThingFilter ___cachedFixedAutoCutFilter)
    {
        if (___cachedFixedAutoCutFilter != null) return;
        
        var biomeGrid = ___map.BiomeGrid();
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return;

        ___cachedFixedAutoCutFilter = new ThingFilter();
        foreach (var allWildPlant in biomeGrid.CellCounts.Keys.SelectMany(b => b.AllWildPlants))
        {
            Log.Message("adding: " + allWildPlant.defName);
            if (allWildPlant.plant.allowAutoCut)
                ___cachedFixedAutoCutFilter.SetAllow(allWildPlant, true);
        }
        ___cachedFixedAutoCutFilter.SetAllow(ThingDefOf.BurnedTree, true);
    }
    
    // ### Modified internal methods ###
    
    private static bool CheckSpawnWildPlantAt_Patched_NonCave(
        WildPlantSpawner instance,
        BiomeDef biome, Map map, IntVec3 c,
        float plantDensity,
        float wholeMapNumDesiredPlants,
        bool setRandomGrowth = false)
    {
        if (plantDensity <= 0.0 || 
            c.GetPlant(map) != null || 
            c.GetCover(map) != null ||
            c.GetEdifice(map) != null || 
            map.fertilityGrid.FertilityAt(c) <= 0.0 ||
            !PlantUtility.SnowAllowsPlanting(c, map)) return false;

        if (SaturatedAt_Patched(instance, biome, map, c, plantDensity, wholeMapNumDesiredPlants)) return false;

        _tsc_possiblePlants ??= new List<ThingDef>();
        _tsc_possiblePlantsWithWeight ??= new List<KeyValuePair<ThingDef, float>>();
        
        CalculatePlantsWhichCanGrowAt_Patched(instance, biome, map, c, _tsc_possiblePlants, plantDensity);
        if (!_tsc_possiblePlants.Any()) return false;
        
        CalculateDistancesToNearbyClusters_Patched(biome, map, c);
        _tsc_possiblePlantsWithWeight.Clear();
        
        foreach (var plant in _tsc_possiblePlants)
        {
            float num = PlantChoiceWeight_Patched(instance, biome, map, plant, c, 
                _tsc_distanceSqToNearbyClusters, wholeMapNumDesiredPlants, plantDensity);
            _tsc_possiblePlantsWithWeight.Add(new KeyValuePair<ThingDef, float>(plant, num));
        }

        if (!_tsc_possiblePlantsWithWeight.TryRandomElementByWeight(x => x.Value, out var result)) return false;
        
        var newThing = (Plant)ThingMaker.MakeThing(result.Key);
        if (setRandomGrowth)
        {
            newThing.Growth = Mathf.Clamp01(InitialGrowthRandomRange.RandomInRange);
            if (newThing.def.plant.LimitedLifespan)
                newThing.Age = Rand.Range(0, Mathf.Max(newThing.def.plant.LifespanTicks - 50, 0));
        }

        GenSpawn.Spawn(newThing, c, map);
        return true;
    }
    
    private static float PlantChoiceWeight_Patched(
        WildPlantSpawner instance,
        BiomeDef biome, Map map,
        ThingDef plantDef, IntVec3 c,
        Dictionary<ThingDef, float> distanceSqToNearbyClusters,
        float wholeMapNumDesiredPlants,
        float plantDensity)
    {
        float comm = GetCommonalityOfPlant(biome, plantDef);
        float commPct = GetCommonalityPctOfPlant(instance, biome, plantDef);
        
        float num1 = comm;
        if (num1 <= 0.0) return num1;

        float x1 = 0.5f;
        if (map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count > wholeMapNumDesiredPlants / 2.0 && !plantDef.plant.cavePlant)
        {
            x1 = map.listerThings.ThingsOfDef(plantDef).Count /
                 (float)map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count / commPct;
            num1 *= GlobalPctSelectionWeightBias.Evaluate(x1);
        }

        if (plantDef.plant.GrowsInClusters && x1 < 1.1)
        {
            float num2 = plantDef.plant.cavePlant ? instance.CavePlantsCommonalitiesSum : biome.PlantCommonalitiesSum;
            float x2 = (float)(comm * (double)plantDef.plant.wildClusterWeight / (num2 - (double)comm + comm * (double)plantDef.plant.wildClusterWeight));
            float outTo1 = (float)(1.0 / (3.14159274101257 * plantDef.plant.wildClusterRadius * plantDef.plant.wildClusterRadius));
            
            float outTo2 = GenMath.LerpDoubleClamped(commPct, 1f, 1f, outTo1, x2);
            if (distanceSqToNearbyClusters.TryGetValue(plantDef, out var f))
            {
                float x3 = Mathf.Sqrt(f);
                num1 *= GenMath.LerpDoubleClamped(
                    plantDef.plant.wildClusterRadius * 0.9f, 
                    plantDef.plant.wildClusterRadius * 1.1f, 
                    plantDef.plant.wildClusterWeight, outTo2, x3);
            }
            else num1 *= outTo2;
        }

        if (plantDef.plant.wildEqualLocalDistribution)
        {
            float f = wholeMapNumDesiredPlants * commPct;
            float a = (float)(Mathf.Max(map.Size.x, map.Size.z) / (double)Mathf.Sqrt(f) * 2.0);
            float radiusToScan = Mathf.Max(a, 7f);
            
            if (plantDef.plant.GrowsInClusters)
                a = Mathf.Max(a, plantDef.plant.wildClusterRadius * 1.6f);
            
            if (radiusToScan <= 25.0)
                num1 *= LocalPlantProportionsWeightFactor_Patched(instance, map, c, commPct, plantDensity, radiusToScan, plantDef);
        }

        return num1;
    }

    private static float LocalPlantProportionsWeightFactor_Patched(
        WildPlantSpawner instance, Map map, IntVec3 c,
        float commonalityPct,
        float plantDensity,
        float radiusToScan,
        ThingDef plantDef)
    {
        float numDesiredPlantsLocally = 0.0f;
        int numPlants = 0;
        int numPlantsThisDef = 0;
        
        RegionTraverser.BreadthFirstTraverse(c, map,
            (_, to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), radiusToScan), reg =>
            {
                numDesiredPlantsLocally += instance.GetDesiredPlantsCountIn(reg, c, plantDensity);
                numPlants += reg.ListerThings.ThingsInGroup(ThingRequestGroup.Plant).Count;
                numPlantsThisDef += reg.ListerThings.ThingsOfDef(plantDef).Count;
                return false;
            });
        
        return numDesiredPlantsLocally * (double)commonalityPct < 2.0 || numPlants <= numDesiredPlantsLocally * 0.5 ? 1f
            : Mathf.Lerp(7f, 1f, numPlantsThisDef / (float)numPlants / commonalityPct);
    }

    private static void CalculatePlantsWhichCanGrowAt_Patched(
        WildPlantSpawner instance,
        BiomeDef biome, Map map, IntVec3 c,
        List<ThingDef> outPlants,
        float plantDensity)
    {
        outPlants.Clear();
        
        var allWildPlants = biome.AllWildPlants;
        foreach (var plantDef in allWildPlants)
        {
            if (plantDef.CanEverPlantAt(c, map))
            {
                if (plantDef.plant.wildOrder != (double)biome.LowestWildAndCavePlantOrder)
                {
                    float num = 7f;
                    if (plantDef.plant.GrowsInClusters)
                        num = Math.Max(num, plantDef.plant.wildClusterRadius * 1.5f);
                    if (!EnoughLowerOrderPlantsNearby_Patched(instance, biome, map, c, plantDensity, num, plantDef))
                        continue;
                }

                outPlants.Add(plantDef);
            }
        }
    }

    private static bool EnoughLowerOrderPlantsNearby_Patched(
        WildPlantSpawner instance,
        BiomeDef biome, Map map, IntVec3 c,
        float plantDensity,
        float radiusToScan,
        ThingDef plantDef)
    {
        float num1 = 0.0f;
        _tsc_plantDefsLowerOrder ??= new List<ThingDef>();
        _tsc_plantDefsLowerOrder.Clear();
        
        var allWildPlants = biome.AllWildPlants;
        foreach (var def in allWildPlants)
        {
            if (def.plant.wildOrder < (double)plantDef.plant.wildOrder)
            {
                num1 += GetCommonalityPctOfPlant(instance, biome, def);
                _tsc_plantDefsLowerOrder.Add(def);
            }
        }

        float numDesiredPlantsLocally = 0.0f;
        int numPlantsLowerOrder = 0;
        RegionTraverser.BreadthFirstTraverse(c, map,
            (_, to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), radiusToScan), reg =>
            {
                numDesiredPlantsLocally += instance.GetDesiredPlantsCountIn(reg, c, plantDensity);
                foreach (var def in _tsc_plantDefsLowerOrder)
                    numPlantsLowerOrder += reg.ListerThings.ThingsOfDef(def).Count;
                return false;
            });
        
        float num2 = numDesiredPlantsLocally * num1;
        return num2 < 4.0 || numPlantsLowerOrder / (double)num2 >= 0.569999992847443;
    }

    private static bool SaturatedAt_Patched(
        WildPlantSpawner instance,
        BiomeDef biome, Map map, IntVec3 c,
        float plantDensity,
        float wholeMapNumDesiredPlants)
    {
        int num = GenRadial.NumCellsInRadius(20f);
        if (wholeMapNumDesiredPlants * (num / (double)map.Area) <= 4.0 
            || (!biome.wildPlantsCareAboutLocalFertility && Rand.ChanceSeeded(BiomeTransition.PlantLowDensityPassChance, c.GetHashCode() ^ map.Tile)))
            return map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count >= (double)wholeMapNumDesiredPlants;
        
        float numDesiredPlantsLocally = 0.0f;
        int numPlants = 0;
        
        RegionTraverser.BreadthFirstTraverse(c, map, (_, to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), 20f),
            reg =>
            {
                numDesiredPlantsLocally += instance.GetDesiredPlantsCountIn(reg, c, plantDensity);
                numPlants += reg.ListerThings.ThingsInGroup(ThingRequestGroup.Plant).Count;
                return false;
            });
        
        return numPlants >= (double)numDesiredPlantsLocally;
    }

    private static void CalculateDistancesToNearbyClusters_Patched(
        BiomeDef biome, Map map, IntVec3 c)
    {
        _tsc_nearbyClusters ??= new Dictionary<ThingDef, List<float>>();
        _tsc_nearbyClustersList ??= new List<KeyValuePair<ThingDef, List<float>>>();
        _tsc_distanceSqToNearbyClusters ??= new Dictionary<ThingDef, float>();

        _tsc_nearbyClusters.Clear();
        _tsc_nearbyClustersList.Clear();
        _tsc_distanceSqToNearbyClusters.Clear();
        
        int num1 = GenRadial.NumCellsInRadius(biome.MaxWildAndCavePlantsClusterRadius * 2);
        for (int index1 = 0; index1 < num1; ++index1)
        {
            var intVec3 = c + GenRadial.RadialPattern[index1];
            if (intVec3.InBounds(map))
            {
                var thingList = map.thingGrid.ThingsListAtFast(intVec3);
                foreach (var thing in thingList)
                {
                    if (thing.def.category == ThingCategory.Plant && thing.def.plant.GrowsInClusters)
                    {
                        float squared = intVec3.DistanceToSquared(c);
                        if (!_tsc_nearbyClusters.TryGetValue(thing.def, out var floatList))
                        {
                            floatList = SimplePool<List<float>>.Get();
                            _tsc_nearbyClusters.Add(thing.def, floatList);
                            _tsc_nearbyClustersList.Add(new KeyValuePair<ThingDef, List<float>>(thing.def, floatList));
                        }

                        floatList.Add(squared);
                    }
                }
            }
        }
        
        foreach (var nearbyClusters in _tsc_nearbyClustersList)
        {
            var floatList = nearbyClusters.Value;
            floatList.Sort();
            double num2 = floatList[floatList.Count / 2];
            _tsc_distanceSqToNearbyClusters.Add(nearbyClusters.Key, (float)num2);
            floatList.Clear();
            SimplePool<List<float>>.Return(floatList);
        }
    }
    
    // ### Extracted values and utils ###
    
    private static readonly FloatRange InitialGrowthRandomRange = new(0.15f, 1.5f);
    
    private static readonly SimpleCurve GlobalPctSelectionWeightBias = new() { new(0.0f, 3f), new(1f, 1f), new(1.5f, 0.25f), new(3f, 0.02f) };
    
    private static float GetCommonalityOfPlant(BiomeDef biome, ThingDef plant)
    {
        return !plant.plant.cavePlant ? biome.CommonalityOfPlant(plant) : plant.plant.cavePlantWeight;
    }

    private static float GetCommonalityPctOfPlant(WildPlantSpawner instance, BiomeDef biome, ThingDef plant)
    {
        return !plant.plant.cavePlant
            ? biome.CommonalityPctOfPlant(plant)
            : GetCommonalityOfPlant(biome, plant) / instance.CavePlantsCommonalitiesSum;
    }
    
    private static float AggregatePlantDensityFactor(GameConditionManager manager, Map map)
    {
        float num = 1f;
        foreach (var t in manager.ActiveConditions)
            num *= t.PlantDensityFactor(map);
        if (manager.Parent != null)
            num *= AggregatePlantDensityFactor(manager.Parent, map);
        return num;
    }
    
    private static bool GoodRoofForCavePlant(Map map, IntVec3 c) => c.GetRoof(map) is { isNatural: true };
    
    private static bool CanRegrowAt(Map map, IntVec3 c) => c.GetTemperature(map) > 0 && (!c.Roofed(map) || GoodRoofForCavePlant(map, c));

    public static void LogInfo(Map map, IntVec3 pos)
    {
        var biomeGrid = map.BiomeGrid();
        var biomeG = biomeGrid.BiomeAt(pos);
        var biomeAS = biomeGrid.BiomeAt(pos, BiomeGrid.BiomeQuery.AnimalSpawning);
        var biomePS = biomeGrid.BiomeAt(pos, BiomeGrid.BiomeQuery.PlantSpawning);
        float plantDensity = biomePS.plantDensity * AggregatePlantDensityFactor(map.gameConditionManager, map);
        Log.Message("pos: " + pos);
        Log.Message("biome: g=" + biomeG.defName + " as=" + biomeAS.defName + " ps=" + biomePS.defName);
        Log.Message("whole map desired: " + map.wildPlantSpawner.CurrentWholeMapNumDesiredPlants);
        Log.Message("desired density at pos: " + plantDensity);
        Log.Message("map open ground fraction: " + map.BiomeGrid()?.OpenGroundFraction);
        Log.Message("map animal density factor: " + GeologicalLandformsAPI.AnimalDensityFactorFunction(map.BiomeGrid()));
    }
}
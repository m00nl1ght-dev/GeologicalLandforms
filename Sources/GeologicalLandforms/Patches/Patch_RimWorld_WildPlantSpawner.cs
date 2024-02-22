using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WildPlantSpawner))]
internal static class Patch_RimWorld_WildPlantSpawner
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WildPlantSpawner);

    [HarmonyTranspiler]
    [HarmonyPatch("CurrentWholeMapNumDesiredPlants", MethodType.Getter)]
    [HarmonyPriority(Priority.VeryLow)]
    private static IEnumerable<CodeInstruction> CurrentWholeMapNumDesiredPlants_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return ConditionalLocalBiomeAware(instructions, generator, nameof(CurrentWholeMapNumDesiredPlants_LocalBiomeAware));
    }

    [HarmonyPatch("CurrentWholeMapNumDesiredPlants", MethodType.Getter)]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static float CurrentWholeMapNumDesiredPlants_LocalBiomeAware(WildPlantSpawner instance, BiomeGrid biomeGrid)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var localCondFactor = generator.DeclareLocal(typeof(float));

            var ldlocPos = new CodeInstruction(OpCodes.Ldloc);

            var getCondFactor = TranspilerPattern.Build("GetCondFactor")
                .MatchCall(typeof(WildPlantSpawner), "get_CurrentPlantDensity").Keep()
                .MatchStloc().Keep()
                .Insert(OpCodes.Ldarg_0)
                .Insert(CodeInstruction.LoadField(typeof(WildPlantSpawner), "map"))
                .Insert(CodeInstruction.LoadField(typeof(Map), "gameConditionManager"))
                .Insert(OpCodes.Ldarg_0)
                .Insert(CodeInstruction.LoadField(typeof(WildPlantSpawner), "map"))
                .Insert(CodeInstruction.Call(typeof(GameConditionManager), "AggregatePlantDensityFactor"))
                .Insert(OpCodes.Stloc, localCondFactor);

            var getPlantDensity = TranspilerPattern.Build("GetPlantDensity")
                .MatchLdloc().StoreOperandIn(ldlocPos).Keep()
                .MatchLdloc().Replace(OpCodes.Ldarg_1)
                .Insert(ldlocPos)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid), nameof(BiomeGrid.BiomeAt), [typeof(IntVec3)]))
                .Insert(CodeInstruction.LoadField(typeof(BiomeDef), "plantDensity"))
                .Insert(OpCodes.Ldloc, localCondFactor)
                .Insert(OpCodes.Mul)
                .MatchCall(typeof(WildPlantSpawner), "GetDesiredPlantsCountAt", [typeof(IntVec3), typeof(IntVec3), typeof(float)]).Keep();

            return TranspilerPattern.Apply(instructions, getCondFactor, getPlantDensity);
        }

        _ = Transpiler(null, null);
        return 0f;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("WildPlantSpawnerTickInternal")]
    [HarmonyPriority(Priority.VeryLow)]
    private static IEnumerable<CodeInstruction> WildPlantSpawnerTickInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return ConditionalLocalBiomeAware(instructions, generator, nameof(WildPlantSpawnerTickInternal_LocalBiomeAware));
    }

    [HarmonyPatch("WildPlantSpawnerTickInternal")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static void WildPlantSpawnerTickInternal_LocalBiomeAware(WildPlantSpawner instance, BiomeGrid biomeGrid)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var localCondFactor = generator.DeclareLocal(typeof(float));
            var localBiomeEntry = generator.DeclareLocal(typeof(BiomeGrid.Entry));

            var stlocDensity = new CodeInstruction(OpCodes.Stloc);
            var ldlocPos = new CodeInstruction(OpCodes.Ldloc);

            var getCondFactor = TranspilerPattern.Build("GetCondFactor")
                .MatchCall(typeof(WildPlantSpawner), "get_CurrentPlantDensity").Keep()
                .MatchStloc().StoreOperandIn(stlocDensity).Keep()
                .Insert(OpCodes.Ldarg_0)
                .Insert(CodeInstruction.LoadField(typeof(WildPlantSpawner), "map"))
                .Insert(CodeInstruction.LoadField(typeof(Map), "gameConditionManager"))
                .Insert(OpCodes.Ldarg_0)
                .Insert(CodeInstruction.LoadField(typeof(WildPlantSpawner), "map"))
                .Insert(CodeInstruction.Call(typeof(GameConditionManager), "AggregatePlantDensityFactor"))
                .Insert(OpCodes.Stloc, localCondFactor);

            var getBiomeEntry = TranspilerPattern.Build("GetBiomeEntry")
                .OnlyMatchAfter(getCondFactor)
                .Match(OpCodes.Ldarg_0).Keep()
                .MatchLoad(typeof(WildPlantSpawner), "map").Keep()
                .MatchLoad(typeof(Map), "cellsInRandomOrder").Keep()
                .Match(OpCodes.Ldarg_0).Keep()
                .MatchLoad(typeof(WildPlantSpawner), "cycleIndex").Keep()
                .MatchCall(typeof(MapCellsInRandomOrder), "Get").Keep()
                .MatchStloc().StoreOperandIn(ldlocPos).Keep()
                .Insert(OpCodes.Ldarg_1)
                .Insert(ldlocPos)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid), nameof(BiomeGrid.EntryAt), [typeof(IntVec3)]))
                .Insert(OpCodes.Stloc, localBiomeEntry)
                .Insert(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid.Entry), "get_Biome"))
                .Insert(CodeInstruction.LoadField(typeof(BiomeDef), "plantDensity"))
                .Insert(OpCodes.Ldloc, localCondFactor)
                .Insert(OpCodes.Mul)
                .Insert(stlocDensity);

            var getBiome = TranspilerPattern.Build("GetBiome")
                .OnlyMatchAfter(getBiomeEntry)
                .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid.Entry), "get_Biome"))
                .MatchLoad(typeof(WildPlantSpawner), "map").Remove()
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .Greedy();

            var checkCaveRoof = TranspilerPattern.Build("GoodRoofForCavePlant")
                .OnlyMatchAfter(getBiomeEntry)
                .MatchCall(typeof(WildPlantSpawner), "GoodRoofForCavePlant").Keep()
                .Insert(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(Self, nameof(ShouldUseVanillaCavePlantLogic)))
                .Match(OpCodes.Brtrue_S).Keep()
                .Greedy();

            var checkSpawn = TranspilerPattern.Build("CheckSpawnWildPlantAt")
                .OnlyMatchAfter(getBiomeEntry)
                .MatchCall(typeof(WildPlantSpawner), "CheckSpawnWildPlantAt").Remove()
                .Insert(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(Self, nameof(CheckSpawnWildPlantAt_LocalBiomeAware)));

            return TranspilerPattern.Apply(instructions, getCondFactor, getBiomeEntry, getBiome, checkCaveRoof, checkSpawn);
        }

        _ = Transpiler(null, null);
    }

    [HarmonyPatch("CheckSpawnWildPlantAt")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    internal static bool CheckSpawnWildPlantAt_LocalBiomeAware(
        WildPlantSpawner instance,
        IntVec3 c, float plantDensity,
        float wholeMapNumDesiredPlants,
        bool setRandomGrowth,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lba0 = ReplaceWithLocalBiomeAwareCall("SaturatedAt", 5);
            var lba1 = ReplaceWithLocalBiomeAwareCall("CalculatePlantsWhichCanGrowAt", 5);
            var lba2 = ReplaceWithLocalBiomeAwareCall("CalculateDistancesToNearbyClusters", 5);
            var lba3 = ReplaceWithLocalBiomeAwareCall("PlantChoiceWeight", 5);

            return TranspilerPattern.Apply(instructions, lba0, lba1, lba2, lba3);
        }

        _ = Transpiler(null);
        return false;
    }

    [HarmonyPatch("PlantChoiceWeight")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static float PlantChoiceWeight_LocalBiomeAware(
        WildPlantSpawner instance,
        ThingDef plantDef, IntVec3 c,
        Dictionary<ThingDef, float> distanceSqToNearbyClusters,
        float wholeMapNumDesiredPlants,
        float plantDensity,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lba0 = ConditionalCavePlantLogic(6);
            var lba1 = ReplaceTileBiomeWithLocalBiome(6);
            var lba2 = ReplaceWithLocalBiomeAwareCall("GetCommonalityOfPlant", 6);
            var lba3 = ReplaceWithLocalBiomeAwareCall("GetCommonalityPctOfPlant", 6);

            return TranspilerPattern.Apply(instructions, lba0, lba1, lba2, lba3);
        }

        _ = Transpiler(null);
        return 0f;
    }

    [HarmonyPatch("CalculatePlantsWhichCanGrowAt")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static void CalculatePlantsWhichCanGrowAt_LocalBiomeAware(
        WildPlantSpawner instance, IntVec3 c,
        List<ThingDef> outPlants,
        bool cavePlants,
        float plantDensity,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var prepend = new List<CodeInstruction>
            {
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg, 5),
                CodeInstruction.Call(Self, nameof(ShouldUseVanillaCavePlantLogic)),
                new(OpCodes.Starg, 3),
            };

            var lba0 = ReplaceTileBiomeWithLocalBiome(5, 2);
            var lba1 = ReplaceWithLocalBiomeAwareCall("EnoughLowerOrderPlantsNearby", 5);

            return prepend.Concat(TranspilerPattern.Apply(instructions, lba0, lba1));
        }

        _ = Transpiler(null);
    }

    [HarmonyPatch("EnoughLowerOrderPlantsNearby")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static bool EnoughLowerOrderPlantsNearby_LocalBiomeAware(
        WildPlantSpawner instance, IntVec3 c,
        float plantDensity,
        float radiusToScan,
        ThingDef plantDef,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lba0 = ReplaceTileBiomeWithLocalBiome(5);
            var lba1 = ReplaceWithLocalBiomeAwareCall("GetCommonalityPctOfPlant", 5);

            return TranspilerPattern.Apply(instructions, lba0, lba1);
        }

        _ = Transpiler(null);
        return false;
    }

    [HarmonyPatch("SaturatedAt")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static bool SaturatedAt_LocalBiomeAware(
        WildPlantSpawner instance, IntVec3 c,
        float plantDensity, bool cavePlants,
        float wholeMapNumDesiredPlants,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
                .Match(OpCodes.Ldarg_0).Keep()
                .MatchLoad(typeof(WildPlantSpawner), "map").Keep()
                .Insert(OpCodes.Ldarg_1)
                .Insert(OpCodes.Ldarg, 5)
                .Insert(CodeInstruction.Call(Self, nameof(ShouldCareAboutLocalFertility)))
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .MatchLoad(typeof(BiomeDef), "wildPlantsCareAboutLocalFertility").Remove();

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
        return false;
    }

    [HarmonyPatch("CalculateDistancesToNearbyClusters")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static void CalculateDistancesToNearbyClusters_LocalBiomeAware(
        WildPlantSpawner instance, IntVec3 c,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return TranspilerPattern.Apply(instructions, ReplaceTileBiomeWithLocalBiome(2));
        }

        _ = Transpiler(null);
    }

    [HarmonyPatch("GetCommonalityOfPlant")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static float GetCommonalityOfPlant_LocalBiomeAware(
        WildPlantSpawner instance, ThingDef plant,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lba0 = ConditionalCavePlantLogic(2);
            var lba1 = ReplaceTileBiomeWithLocalBiome(2);

            return TranspilerPattern.Apply(instructions, lba0, lba1);
        }

        _ = Transpiler(null);
        return 0f;
    }

    [HarmonyPatch("GetCommonalityPctOfPlant")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static float GetCommonalityPctOfPlant_LocalBiomeAware(
        WildPlantSpawner instance, ThingDef plant,
        BiomeGrid.Entry entry)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lba0 = ConditionalCavePlantLogic(2);
            var lba1 = ReplaceTileBiomeWithLocalBiome(2);
            var lba2 = ReplaceWithLocalBiomeAwareCall("GetCommonalityOfPlant", 2);

            return TranspilerPattern.Apply(instructions, lba0, lba1, lba2);
        }

        _ = Transpiler(null);
        return 0f;
    }

    private static IEnumerable<CodeInstruction> ConditionalLocalBiomeAware(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        string destMethod)
    {
        var labelVanillaImpl = generator.DefineLabel();
        var localBiomeGrid = generator.DeclareLocal(typeof(BiomeGrid));

        var conditionalLocalBiomeAware = new List<CodeInstruction>
        {
            new(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(WildPlantSpawner), "map"),
            CodeInstruction.Call(typeof(ExtensionUtils), nameof(ExtensionUtils.BiomeGrid)),
            new(OpCodes.Stloc, localBiomeGrid),
            new(OpCodes.Ldloc, localBiomeGrid),
            new(OpCodes.Brfalse_S, labelVanillaImpl),
            new(OpCodes.Ldloc, localBiomeGrid),
            CodeInstruction.Call(typeof(BiomeGrid), "get_Enabled"),
            new(OpCodes.Brfalse_S, labelVanillaImpl),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldloc, localBiomeGrid),
            CodeInstruction.Call(Self, destMethod),
            new(OpCodes.Ret)
        };

        instructions.First().labels.Add(labelVanillaImpl);
        return conditionalLocalBiomeAware.Concat(instructions);
    }

    private static TranspilerPattern ReplaceTileBiomeWithLocalBiome(int entryArg, int minExpected = 1) =>
        TranspilerPattern.Build("MakeAwareOfLocalBiome")
            .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldarg, entryArg)
            .Insert(CodeInstruction.Call(typeof(BiomeGrid.Entry), "get_Biome"))
            .MatchLoad(typeof(WildPlantSpawner), "map").Remove()
            .MatchCall(typeof(Map), "get_Biome").Remove()
            .Greedy(minExpected);

    private static TranspilerPattern ConditionalCavePlantLogic(int entryArg, int minExpected = 1) =>
        TranspilerPattern.Build("ConditionalCavePlantLogic")
            .MatchLoad(typeof(PlantProperties), "cavePlant").Keep()
            .Insert(OpCodes.Ldarg, entryArg)
            .Insert(CodeInstruction.Call(Self, nameof(ShouldUseVanillaCavePlantLogic)))
            .Greedy(minExpected);

    private static TranspilerPattern ReplaceWithLocalBiomeAwareCall(string method, int entryArg, int minExpected = 1) =>
        TranspilerPattern.Build(method)
            .MatchCall(typeof(WildPlantSpawner), method).Replace(OpCodes.Ldarg, entryArg)
            .Insert(CodeInstruction.Call(Self, method + "_LocalBiomeAware"))
            .Greedy(minExpected);

    private static bool ShouldCareAboutLocalFertility(Map map, IntVec3 c, BiomeGrid.Entry entry)
    {
        if (entry.Biome.wildPlantsCareAboutLocalFertility) return true;
        return !Rand.ChanceSeeded(BiomeTransition.PlantLowDensityPassChance, c.GetHashCode() ^ map.Tile);
    }

    private static bool ShouldUseVanillaCavePlantLogic(bool underCaveRoof, BiomeGrid.Entry entry)
    {
        return underCaveRoof && !entry.ApplyToCaves;
    }
}

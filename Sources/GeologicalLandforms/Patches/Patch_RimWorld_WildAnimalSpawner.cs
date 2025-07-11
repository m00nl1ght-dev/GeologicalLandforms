#if !RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WildAnimalSpawner))]
internal static class Patch_RimWorld_WildAnimalSpawner
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WildAnimalSpawner);

    [HarmonyTranspiler]
    [HarmonyPatch("DesiredAnimalDensity", MethodType.Getter)]
    [HarmonyPriority(Priority.VeryLow)]
    private static IEnumerable<CodeInstruction> DesiredAnimalDensity_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
            .TrimBefore()
            .Insert(OpCodes.Ldarg_0)
            .Insert(OpCodes.Ldarg_0)
            .Insert(CodeInstruction.LoadField(typeof(WildAnimalSpawner), "map"))
            .Insert(CodeInstruction.Call(Self, nameof(DesiredAnimalDensity_Base)))
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WildAnimalSpawner), "map").Keep()
            .MatchLoad(typeof(Map), "gameConditionManager").Keep();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyPatch("DesiredAnimalDensity", MethodType.Getter)]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static float DesiredAnimalDensity_Base_ForBiome(WildAnimalSpawner instance, BiomeDef biome)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var trimmerPattern = TranspilerPattern.Build("TrimToBase")
                .Match(OpCodes.Ldarg_0).Remove()
                .MatchLoad(typeof(WildAnimalSpawner), "map").Remove()
                .MatchLoad(typeof(Map), "gameConditionManager").Remove()
                .TrimAfter()
                .Insert(OpCodes.Ret);

            var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
                .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldarg_1)
                .MatchLoad(typeof(WildAnimalSpawner), "map").Remove()
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .Greedy();

            var trimmed = TranspilerPattern.Apply(instructions, trimmerPattern);
            return TranspilerPattern.Apply(trimmed, pattern);
        }

        _ = Transpiler(null);
        return 0f;
    }

    private static float DesiredAnimalDensity_Base(WildAnimalSpawner instance, Map map)
    {
        var density = 0f;
        var biomeGrid = map.BiomeGrid();

        if (biomeGrid is { Enabled: true })
        {
            float cells = map.cellIndices.NumGridCells;

            foreach (var entry in biomeGrid.Entries)
            {
                var val = DesiredAnimalDensity_Base_ForBiome(instance, entry.Biome);
                density += val * (entry.CellCount / cells);
            }
        }
        else
        {
            density = DesiredAnimalDensity_Base_ForBiome(instance, map.Biome);
        }

        return density * GeologicalLandformsAPI.AnimalDensityFactor.Apply(biomeGrid);
    }

    private static BiomeDef _localBiome;

    private static BiomeDef LocalBiome(Map map) => _localBiome ?? map.Biome;

    [HarmonyPrefix]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    private static void SpawnRandomWildAnimalAt_Prefix(Map ___map, IntVec3 loc)
    {
        var biomeGrid = ___map.BiomeGrid();
        _localBiome = biomeGrid is { Enabled: true } ? biomeGrid.BiomeAt(loc) : ___map.Biome;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    private static void SpawnRandomWildAnimalAt_Postfix()
    {
        _localBiome = null;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> SpawnRandomWildAnimalAt_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WildAnimalSpawner), "map").Keep()
            .MatchCall(typeof(Map), "get_Biome").Remove()
            .Insert(CodeInstruction.Call(Self, nameof(LocalBiome)))
            .MatchCall(typeof(BiomeDef), "get_AllWildAnimals").Keep();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyTranspiler]
    [HarmonyPatch("CommonalityOfAnimalNow")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> CommonalityOfAnimalNow_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WildAnimalSpawner), "map").Keep()
            .MatchCall(typeof(Map), "get_Biome").Remove()
            .Insert(CodeInstruction.Call(Self, nameof(LocalBiome)))
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }
}

#endif

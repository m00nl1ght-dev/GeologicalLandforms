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

        return density * GeologicalLandformsAPI.AnimalDensityFactor(biomeGrid);
    }

    [HarmonyTranspiler]
    [HarmonyPatch("SpawnRandomWildAnimalAt")]
    [HarmonyPriority(Priority.Low)]
    [HarmonyAfter("net.mseal.rimworld.mod.terrain.movement")]
    private static IEnumerable<CodeInstruction> SpawnRandomWildAnimalAt_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("AdjustPotentialAnimalSpawns")
            .Insert(OpCodes.Ldarg_0)
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WildAnimalSpawner), "map").Keep()
            .MatchCall(typeof(Map), "get_Biome").Remove()
            .MatchCall(typeof(BiomeDef), "get_AllWildAnimals").Remove()
            .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldarg_1)
            .Match(OpCodes.Ldftn).Remove()
            .Match(OpCodes.Newobj).Remove()
            .Match(OpCodes.Call).Remove()
            .Match(OpCodes.Ldarg_0).Remove()
            .Match(OpCodes.Ldftn).Remove()
            .Match(OpCodes.Newobj).Remove()
            .Match(OpCodes.Ldloca_S).Keep()
            .Match(OpCodes.Call).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(AdjustPotentialAnimalSpawns)))
            .Match(OpCodes.Brtrue_S).Keep();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyPatch("CommonalityOfAnimalNow")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.Last)]
    private static float CommonalityOfAnimalNow_LocalBiomeAware(WildAnimalSpawner instance, PawnKindDef def, BiomeDef biome)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pattern = TranspilerPattern.Build("MakeAwareOfLocalBiome")
                .Match(OpCodes.Ldarg_0).Replace(OpCodes.Ldarg_2)
                .MatchLoad(typeof(WildAnimalSpawner), "map").Remove()
                .MatchCall(typeof(Map), "get_Biome").Remove()
                .Greedy();

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
        return 0f;
    }

    private static bool AdjustPotentialAnimalSpawns(WildAnimalSpawner instance, Map map, IntVec3 loc, out PawnKindDef result)
    {
        var biomeGrid = map.BiomeGrid();
        var biome = biomeGrid is { Enabled: true } ? biomeGrid.BiomeAt(loc) : map.Biome;

        return biome.AllWildAnimals
            .Where(a => map.mapTemperature.SeasonAcceptableFor(a.race))
            .TryRandomElementByWeight(def => CommonalityOfAnimalNow_LocalBiomeAware(instance, def, biome), out result);
    }
}

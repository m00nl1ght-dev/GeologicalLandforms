using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Plants))]
internal static class Patch_RimWorld_GenStep_Plants
{
    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryLow)]
    private static bool Generate_Prefix(GenStep_Plants __instance, Map map, GenStepParams parms)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        float condFactor = map.gameConditionManager.AggregatePlantDensityFactor(map);
        Generate_LocalBiomeAware(__instance, map, parms, biomeGrid, condFactor);
        return false;
    }

    [HarmonyPatch("Generate")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static void Generate_LocalBiomeAware(GenStep_Plants instance, Map map, GenStepParams parms, BiomeGrid biomeGrid, float condFactor)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var localBiomeEntry = generator.DeclareLocal(typeof(BiomeGrid.Entry));

            var ldlocPos = new CodeInstruction(OpCodes.Ldloc);

            var pattern = TranspilerPattern.Build("GetPlantDensity")
                .MatchLdloc().StoreOperandIn(ldlocPos).Keep()
                .MatchLdloc().Replace(OpCodes.Ldarg_3)
                .Insert(ldlocPos)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid), nameof(BiomeGrid.EntryAt), [typeof(IntVec3)]))
                .Insert(OpCodes.Stloc, localBiomeEntry)
                .Insert(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(typeof(BiomeGrid.Entry), "get_Biome"))
                .Insert(CodeInstruction.LoadField(typeof(BiomeDef), "plantDensity"))
                .Insert(OpCodes.Ldarg, 4)
                .Insert(OpCodes.Mul)
                .MatchLdloc().Keep()
                .MatchAny().Keep()
                .MatchCall(typeof(WildPlantSpawner), "CheckSpawnWildPlantAt").Remove()
                .Insert(OpCodes.Ldloc, localBiomeEntry)
                .Insert(CodeInstruction.Call(typeof(Patch_RimWorld_WildPlantSpawner), "CheckSpawnWildPlantAt_LocalBiomeAware"));

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null, null);
    }
}

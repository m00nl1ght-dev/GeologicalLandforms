#if !RW_1_6_OR_GREATER

using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
public class ModCompat_BiomesCore : ModCompat
{
    public override string TargetAssemblyName => "BiomesCore";
    public override string DisplayName => "Biomes! Core";

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.MapGeneration.GenStep_Island", "Generate")]
    private static bool GenStep_Island_Prefix() => Landform.GetFeature(lf => lf.OutputFertility?.Get()) == null;

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.MapGeneration.GenStep_IslandElevation", "Generate")]
    private static bool GenStep_IslandElevation_Prefix() => Landform.GetFeature(lf => lf.OutputElevation?.Get()) == null;

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.MapGeneration.GenStep_Cavern", "Generate")]
    private static bool GenStep_Cavern_Prefix() => Landform.GetFeature(lf => lf.OutputElevation?.Get()) == null;

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.Patches.LocalBiomeExtensionPoint", "LocalBiome")]
    private static bool LocalBiomeExtensionPoint(Map map, IntVec3 pos, ref BiomeDef __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;
        __result = biomeGrid.BiomeAt(pos);
        return false;
    }
}

#endif

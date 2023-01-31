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
    [HarmonyPatch("BiomesCore.Patches.LocalBiomeExtensionPoint", "LocalBiome")]
    private static bool LocalBiomeExtensionPoint(Map map, IntVec3 pos, ref BiomeDef __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;
        __result = biomeGrid.BiomeAt(pos);
        return false;
    }
}

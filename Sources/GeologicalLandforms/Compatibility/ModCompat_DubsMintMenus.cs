#if !RW_1_6_OR_GREATER

using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_DubsMintMenus : ModCompat
{
    public override string TargetAssemblyName => "DubsMintMenus";
    public override string DisplayName => "Dubs Mint Menus";

    [HarmonyPrefix]
    [HarmonyPatch("DubsMintMenus.Dialog_FancyDanPlantSetterBob", "IsPlantAvailable")]
    private static bool Dialog_FancyDanPlantSetterBob_IsPlantAvailable(ThingDef plantDef, Map map, ref bool __result)
    {
        var biomeGrid = map.BiomeGrid();
        if (biomeGrid is not { Enabled: true }) return true;

        __result = Patch_Verse_Command_SetPlantToGrow.IsPlantAvailable_LocalBiomeAware(plantDef, map, biomeGrid);
        return false;
    }
}

#endif

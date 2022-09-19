using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

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
        if (biomeGrid is not { ShouldApplyForPlantSpawning: true }) return true;

        if (!plantDef.plant.mustBeWildToSow) return true;

        var researchPrerequisites = plantDef.plant.sowResearchPrerequisites;
        if (researchPrerequisites != null && Enumerable.Any(researchPrerequisites, project => !project.IsFinished))
            return true;

        __result = biomeGrid.CellCounts.Keys.SelectMany(b => b.AllWildPlants).Contains(plantDef);
        return false;
    }
}
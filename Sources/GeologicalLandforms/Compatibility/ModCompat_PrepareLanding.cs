using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_PrepareLanding : ModCompat
{
    public override string TargetAssemblyName => "PrepareLanding";
    public override string DisplayName => "Prepare Landing";

    private static MethodInfo _methodUtilEntryHeader;
    private static PropertyInfo _propertyUtilListing;

    private static Landform _landformFilter;

    private static bool _impOptionFlag;

    protected override bool OnApply()
    {
        var utilType = FindType("PrepareLanding.Core.Gui.Tab.TabGuiUtility");

        _methodUtilEntryHeader = Require(AccessTools.Method(utilType, "DrawEntryHeader"));
        _propertyUtilListing = Require(AccessTools.Property(utilType, "ListingStandard"));

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.Filters.TileFilterHasCave", "Filter")]
    private static void TileFilterHasCave_Filter(ref List<int> ____filteredTiles)
    {
        if (_landformFilter == null) return;
        ____filteredTiles.RemoveAll(tile =>
        {
            var landforms = WorldTileInfo.Get(tile).Landforms;
            return landforms == null || !landforms.Contains(_landformFilter);
        });
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.Filters.TileFilterHasCave", "IsFilterActive", MethodType.Getter)]
    private static void TileFilterHasCave_FilterActive(ref bool __result)
    {
        __result = __result || _landformFilter != null;
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.TabTerrain", "DrawBiomeTypesSelection")]
    private static void TabTerrain_DrawBiomeTypesSelection(object ____gameData)
    {
        if (!_impOptionFlag)
        {
            _impOptionFlag = true;
            Traverse.Create(____gameData).Property("UserData").Property("Options").Field("_allowImpassableHilliness").SetValue(true);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.TabTemperature", "DrawCaveSelection")]
    private static void TabTemperature_DrawCaveSelection(object __instance)
    {
        _methodUtilEntryHeader.Invoke(__instance, [
            (string) "GeologicalLandforms.Integration.PrepareLanding.Title".Translate(),
            true, false, Color.magenta, 0.2f
        ]);

        var listingStandard = (Listing_Standard) _propertyUtilListing.GetValue(__instance);

        if (listingStandard.ButtonText("GeologicalLandforms.WorldMap.SelectLandform".Translate()))
        {
            List<FloatMenuOption> floatMenuOptions =
            [
                new FloatMenuOption("PLMW_SelectAny".Translate(), () =>
                {
                    _landformFilter = null;
                })
            ];

            foreach (var landform in LandformManager.LandformsById.Values
                .Where(l => !l.IsInternal)
                .OrderBy(e => e.TranslatedNameForSelection))
            {
                floatMenuOptions.Add(new FloatMenuOption(landform.TranslatedNameForSelection.CapitalizeFirst(), () =>
                {
                    _landformFilter = landform;
                }));
            }

            FloatMenu floatMenu = new(floatMenuOptions, "GeologicalLandforms.WorldMap.SelectLandform".Translate());
            Find.WindowStack.Add(floatMenu);
        }

        var rightLabel = _landformFilter != null
            ? _landformFilter.TranslatedNameForSelection.CapitalizeFirst()
            : (string) "PLMW_SelectAny".Translate();

        listingStandard.LabelDouble($"{"GeologicalLandforms.WorldMap.Landform".Translate()}:", rightLabel);
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.GameData.UserData", "ResetAllFields")]
    private static void UserData_ResetAllFields()
    {
        _landformFilter = null;
    }
}

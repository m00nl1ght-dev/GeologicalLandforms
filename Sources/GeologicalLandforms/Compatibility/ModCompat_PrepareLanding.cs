using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Compatibility;

internal class ModCompat_PrepareLanding : ModCompat
{
    public override string TargetAssemblyName => "PrepareLanding";
    public override string DisplayName => "Prepare Landing";
    
    private static MethodInfo MethodUtilEntryHeader;
    private static PropertyInfo PropertyUtilListing;

    private static Landform LandformFilter;

    protected override bool OnApply()
    {
        var utilType = FindType("PrepareLanding.Core.Gui.Tab.TabGuiUtility");
        
        MethodUtilEntryHeader = Require(AccessTools.Method(utilType, "DrawEntryHeader"));
        PropertyUtilListing = Require(AccessTools.Property(utilType, "ListingStandard"));
        
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.Filters.TileFilterHasCave", "Filter")]
    private static void TileFilterHasCave_Filter(ref List<int> ____filteredTiles)
    {
        if (LandformFilter == null) return;
        ____filteredTiles.RemoveAll(tile =>
        {
            var landforms = WorldTileInfo.Get(tile).Landforms;
            return landforms == null || !landforms.Contains(LandformFilter);
        });
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.Filters.TileFilterHasCave", "IsFilterActive", MethodType.Getter)]
    private static void TileFilterHasCave_FilterActive(ref bool __result)
    {
        __result = __result || LandformFilter != null;
    }

    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.TabTemperature", "DrawCaveSelection")]
    private static void TabTemperature_DrawCaveSelection(object __instance)
    {
        MethodUtilEntryHeader.Invoke(__instance, new object[]
        {
            (string) "GeologicalLandforms.Integration.PrepareLanding.Title".Translate(),
            true, false, Color.magenta, 0.2f
        });

        var listingStandard = (Listing_Standard) PropertyUtilListing.GetValue(__instance);
        
        if (listingStandard.ButtonText("GeologicalLandforms.WorldMap.SelectLandform".Translate()))
        {
            List<FloatMenuOption> floatMenuOptions = new()
            {
                new FloatMenuOption("PLMW_SelectAny".Translate(), () =>
                {
                    LandformFilter = null;
                })
            };

            foreach (var landform in LandformManager.Landforms.Values.Where(l => !l.IsLayer).OrderBy(e => e.TranslatedNameForSelection))
            {
                floatMenuOptions.Add(new FloatMenuOption(landform.TranslatedNameForSelection.CapitalizeFirst(), () =>
                {
                    LandformFilter = landform;
                }));
            }

            FloatMenu floatMenu = new(floatMenuOptions, "GeologicalLandforms.WorldMap.SelectLandform".Translate());
            Find.WindowStack.Add(floatMenu);
        }

        var rightLabel = LandformFilter != null
            ? LandformFilter.TranslatedNameForSelection.CapitalizeFirst()
            : (string) "PLMW_SelectAny".Translate();
        
        listingStandard.LabelDouble($"{"GeologicalLandforms.WorldMap.Landform".Translate()}:", rightLabel);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("PrepareLanding.GameData.UserData", "ResetAllFields")]
    private static void UserData_ResetAllFields()
    {
        LandformFilter = null;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming
namespace GeologicalLandforms.ModCompat;

[StaticConstructorOnStartup]
internal static class ModCompat_PrepareLanding
{
    private static readonly MethodInfo MethodUtilEntryHeader;
    private static readonly PropertyInfo PropertyUtilListing;

    private static Landform LandformFilter;

    static ModCompat_PrepareLanding()
    {
        try
        {
            Type tabType = GenTypes.GetTypeInAnyAssembly("PrepareLanding.TabTemperature");
            if (tabType != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for PrepareLanding.");
                Harmony harmony = new("Geological Landforms PrepareLanding Compat");
                
                Type utilType = GenTypes.GetTypeInAnyAssembly("PrepareLanding.Core.Gui.Tab.TabGuiUtility");
                Type filterType = GenTypes.GetTypeInAnyAssembly("PrepareLanding.Filters.TileFilterHasCave");
                Type userDataType = GenTypes.GetTypeInAnyAssembly("PrepareLanding.GameData.UserData");
                
                MethodUtilEntryHeader = AccessTools.Method(utilType, "DrawEntryHeader");
                PropertyUtilListing = utilType.GetProperty("ListingStandard");

                MethodInfo methodDraw = AccessTools.Method(tabType, "DrawCaveSelection");
                MethodInfo methodFilter = AccessTools.Method(filterType, "Filter");
                MethodInfo methodFilterActive = AccessTools.Method(filterType, "get_IsFilterActive");
                MethodInfo methodReset = AccessTools.Method(userDataType, "ResetAllFields");

                Type self = typeof(ModCompat_PrepareLanding);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod methodDrawPostfix = new(self.GetMethod(nameof(TabTemperature_Draw_Postfix), bindingFlags));
                HarmonyMethod methodFilterPostfix = new(self.GetMethod(nameof(TileFilterHasCave_Filter_Postfix), bindingFlags));
                HarmonyMethod methodFilterActivePostfix = new(self.GetMethod(nameof(TileFilterHasCave_FilterActive_Postfix), bindingFlags));
                HarmonyMethod methodResetPostfix = new(self.GetMethod(nameof(UserData_ResetAllFields_Postfix), bindingFlags));

                harmony.Patch(methodDraw, null, methodDrawPostfix);
                harmony.Patch(methodFilter, null, methodFilterPostfix);
                harmony.Patch(methodFilterActive, null, methodFilterActivePostfix);
                harmony.Patch(methodReset, null, methodResetPostfix);
            }
        }
        catch
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for PrepareLanding!");
        }
    }

    private static void TileFilterHasCave_Filter_Postfix(ref List<int> ____filteredTiles)
    {
        if (LandformFilter == null) return;
        ____filteredTiles.RemoveAll(tile =>
        {
            var landforms = WorldTileInfo.Get(tile).Landforms;
            return landforms == null || !landforms.Contains(LandformFilter);
        });
    }
    
    private static void TileFilterHasCave_FilterActive_Postfix(ref bool __result)
    {
        __result = __result || LandformFilter != null;
    }

    private static void TabTemperature_Draw_Postfix(object __instance)
    {
        MethodUtilEntryHeader.Invoke(__instance, new object[]
        {
            (string) "GeologicalLandforms.Integration.PrepareLanding.Title".Translate(),
            true, false, Color.magenta, 0.2f
        });

        Listing_Standard listingStandard = (Listing_Standard) PropertyUtilListing.GetValue(__instance);
        
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
    
    private static void UserData_ResetAllFields_Postfix()
    {
        LandformFilter = null;
    }
}
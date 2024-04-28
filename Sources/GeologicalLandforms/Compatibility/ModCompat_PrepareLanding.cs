using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_PrepareLanding : ModCompat
{
    private static readonly Type Self = typeof(ModCompat_PrepareLanding);

    public override string TargetAssemblyName => "PrepareLanding";
    public override string DisplayName => "Prepare Landing";

    private const string XElementName = "GL_Landform";

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

    [HarmonyTranspiler]
    [HarmonyPatch("PrepareLanding.Presets.Preset", "LoadPreset")]
    private static IEnumerable<CodeInstruction> LoadPreset_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ldlocElement = new CodeInstruction(OpCodes.Ldloc);

        var userDataType = AccessTools.TypeByName("PrepareLanding.GameData.UserData");

        var pattern = TranspilerPattern.Build("LoadPreset")
            .MatchLdloc().StoreOperandIn(ldlocElement).Keep()
            .Match(OpCodes.Ldstr).Keep()
            .Match(OpCodes.Call).Keep()
            .MatchCall(userDataType, "set_HasCaveState").Keep()
            .Insert(ldlocElement)
            .Insert(CodeInstruction.Call(Self, nameof(LoadPresetExt)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    [HarmonyTranspiler]
    [HarmonyPatch("PrepareLanding.Presets.Preset", "SavePreset")]
    private static IEnumerable<CodeInstruction> SavePreset_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ldlocElement = new CodeInstruction(OpCodes.Ldloc);

        var userDataType = AccessTools.TypeByName("PrepareLanding.GameData.UserData");

        var pattern = TranspilerPattern.Build("LoadPreset")
            .MatchLdloc().StoreOperandIn(ldlocElement).Keep()
            .Match(OpCodes.Ldstr).Keep()
            .MatchAny().Keep().MatchAny().Keep().MatchAny().Keep()
            .MatchCall(userDataType, "get_HasCaveState").Keep()
            .Match(OpCodes.Call).Keep()
            .Insert(ldlocElement)
            .Insert(CodeInstruction.Call(Self, nameof(SavePresetExt)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static void LoadPresetExt(XElement parent)
    {
        var landformId = parent.Element(XElementName)?.Value;
        _landformFilter = landformId == null ? null : LandformManager.FindById(landformId);
    }

    private static void SavePresetExt(XElement parent)
    {
        if (_landformFilter != null)
        {
            parent.Add(new XElement(XElementName, _landformFilter.Id));
        }
    }
}

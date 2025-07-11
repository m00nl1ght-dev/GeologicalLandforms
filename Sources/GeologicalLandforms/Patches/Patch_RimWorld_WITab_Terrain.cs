using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using UnityEngine;
using Verse;

#if !RW_1_6_OR_GREATER
using System.Text;
#endif

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WITab_Terrain))]
internal static class Patch_RimWorld_WITab_Terrain
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WITab_Terrain);

    private static int _tileId;
    private static IWorldTileInfo _tile;

    [HarmonyPrefix]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static void FillTab_Prefix()
    {
        _tileId = Find.WorldSelector.selectedTile;
        _tile = _tileId >= 0 ? WorldTileInfo.Get(_tileId) : null;
    }

    #if RW_1_6_OR_GREATER

    [HarmonyTranspiler]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillTab_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patternBl = TranspilerPattern.Build("BiomeLabelExt")
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchCall(typeof(WITab), "get_SelTile").Keep()
            .MatchCall(typeof(Tile), "get_PrimaryBiome").Keep()
            .MatchCall(typeof(Def), "get_LabelCap").Keep()
            .MatchCall(typeof(Widgets), "Label", [typeof(Rect), typeof(TaggedString)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoBiomeLabel)));

        return TranspilerPattern.Apply(instructions, patternBl);
    }

    [HarmonyTranspiler]
    [HarmonyPatch("DrawScrollContents")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> DrawScrollContents_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patternBd = TranspilerPattern.Build("BiomeDescriptionExt")
            .MatchCall(typeof(Tile), "get_PrimaryBiome").Keep()
            .MatchLoad(typeof(Def), "description").Keep()
            .Match(OpCodes.Ldc_R4).Keep()
            .MatchAny().Keep().MatchAny().Keep().MatchAny().Keep()
            .MatchCall(typeof(Listing_Standard), "Label", [typeof(string), typeof(float), typeof(TipSignal)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoBiomeDescription)));

        return TranspilerPattern.Apply(instructions, patternBd);
    }

    [HarmonyPostfix]
    [HarmonyPatch("ListMiscDetails")]
    private static void ListMiscDetails_Postfix(Listing_Standard listing)
    {
        listing.GetRect(10f);
        GeologicalLandformsAPI.TerrainTabUI.Apply(listing);
    }

    [HarmonyPostfix]
    [HarmonyPatch("ListGeometricDetails")]
    private static void ListGeometricDetails_Postfix(Listing_Standard listing)
    {
        if (_tile is { Landforms: not null, BorderingBiomes.Count: > 0 })
        {
            if (_tile.Landforms.Any(l => l.OutputBiomeGrid?.BiomeTransitionKnob?.connected() ?? false))
            {
                string bbStr = _tile.BorderingBiomes.Select(b => b.Biome.label.CapitalizeFirst()).Distinct().Join(b => b);
                listing.LabelDouble("GeologicalLandforms.WorldMap.BorderingBiomes".Translate(), bbStr);
            }
        }
    }

    #else

    [HarmonyTranspiler]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillTab_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patternBl = TranspilerPattern.Build("BiomeLabelExt")
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchCall(typeof(WITab), "get_SelTile").Keep()
            .MatchLoad(typeof(Tile), "biome").Keep()
            .MatchCall(typeof(Def), "get_LabelCap").Keep()
            .MatchCall(typeof(Widgets), "Label", [typeof(Rect), typeof(TaggedString)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoBiomeLabel)));

        var patternBd = TranspilerPattern.Build("BiomeDescriptionExt")
            .OnlyMatchAfter(patternBl)
            .MatchLoad(typeof(Tile), "biome").Keep()
            .MatchLoad(typeof(Def), "description").Keep()
            .Match(OpCodes.Ldc_R4).Keep()
            .Match(OpCodes.Ldnull).Keep()
            .MatchCall(typeof(Listing_Standard), "Label", [typeof(string), typeof(float), typeof(string)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoBiomeDescription)));

        var patternSfc = TranspilerPattern.Build("SpecialFeaturesCond")
            .OnlyMatchAfter(patternBd)
            .MatchCall(typeof(StringBuilder), "get_Length").Keep()
            .Match(OpCodes.Ldc_I4_0).Replace(OpCodes.Ldc_I4_M1)
            .Match(OpCodes.Ble_S).Keep()
            .MatchAny().Keep()
            .MatchConst("SpecialFeatures").Keep();

        var patternSfr = TranspilerPattern.Build("SpecialFeaturesExt")
            .OnlyMatchAfter(patternSfc)
            .MatchCall(typeof(Listing_Standard), "LabelDouble", [typeof(string), typeof(string), typeof(string)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoSpecialFeatures)));

        return TranspilerPattern.Apply(instructions, patternBl, patternBd, patternSfc, patternSfr);
    }

    private static void DoSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        if (_tile?.Landforms != null)
        {
            var str = _tile.Landforms
                .Where(lf => !lf.IsInternal)
                .OrderBy(lf => lf.Manifest.TimeCreated)
                .Select(lf => lf.TranslatedNameWithDirection(_tile.TopologyDirection).CapitalizeFirst())
                .Join();

            if (str.Length > 0)
            {
                listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Landform".Translate(), str);
            }

            if (_tile.BorderingBiomes?.Count > 0)
            {
                if (_tile.Landforms.Any(l => l.OutputBiomeGrid?.BiomeTransitionKnob?.connected() ?? false))
                {
                    string bbStr = _tile.BorderingBiomes.Select(b => b.Biome.label.CapitalizeFirst()).Distinct().Join(b => b);
                    listingStandard.LabelDouble("GeologicalLandforms.WorldMap.BorderingBiomes".Translate(), bbStr);
                }
            }
        }

        StringBuilder sb = new();

        if (_tileId >= 0 && Find.World.HasCaves(_tileId))
        {
            sb.AppendWithComma("HasCaves".Translate());
        }

        if (sb.Length > 0)
            listingStandard.LabelDouble("SpecialFeatures".Translate(), sb.ToString().CapitalizeFirst());

        listingStandard.Gap();

        GeologicalLandformsAPI.TerrainTabUI.Apply(listingStandard);
    }

    #endif

    private static void DoBiomeLabel(Rect rect, TaggedString labelBase)
    {
        var label = labelBase.Resolve().UncapitalizeFirst();

        if (_tile?.BiomeVariants != null)
        {
            label = _tile.BiomeVariants.Aggregate(label, (current, variant) => variant.ApplyToBiomeLabel(_tile, current));
        }

        Widgets.Label(rect, label.CapitalizeFirst());
    }

    #if RW_1_6_OR_GREATER
    private static Rect DoBiomeDescription(Listing_Standard listingStandard, string description, float float0, TipSignal tipSignal)
    #else
    private static Rect DoBiomeDescription(Listing_Standard listingStandard, string description, float float0, string str0)
    #endif
    {
        if (_tile?.BiomeVariants != null)
        {
            description = _tile.BiomeVariants.Aggregate(description, (current, variant) => variant.ApplyToBiomeDescription(_tile, current));
        }

        return listingStandard.Label(description);
    }
}

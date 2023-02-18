using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WITab_Terrain))]
internal static class Patch_RimWorld_WITab_Terrain
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WITab_Terrain);
    
    private static int _tileId;
    private static WorldTileInfo _tile;

    [HarmonyPrefix]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static void FillTab_Prefix()
    {
        _tileId = Find.WorldSelector.selectedTile;
        _tile = WorldTileInfo.Get(_tileId);
    }

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
            .MatchCall(typeof(Widgets), "Label", new[] { typeof(Rect), typeof(TaggedString) }).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoBiomeLabel)));

        var patternBd = TranspilerPattern.Build("BiomeDescriptionExt")
            .OnlyMatchAfter(patternBl)
            .MatchLoad(typeof(Tile), "biome").Keep()
            .MatchLoad(typeof(Def), "description").Keep()
            .Match(OpCodes.Ldc_R4).Keep()
            .Match(OpCodes.Ldnull).Keep()
            .MatchCall(typeof(Listing_Standard), "Label", new[] { typeof(string), typeof(float), typeof(string) }).Remove()
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
            .MatchCall(typeof(Listing_Standard), "LabelDouble", new[] { typeof(string), typeof(string), typeof(string) }).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(DoSpecialFeatures)));

        return TranspilerPattern.Apply(instructions, patternBl, patternBd, patternSfc, patternSfr);
    }

    private static void DoBiomeLabel(Rect rect, TaggedString labelBase)
    {
        var label = labelBase.Resolve().UncapitalizeFirst();

        if (_tile.BiomeVariants != null)
        {
            label = _tile.BiomeVariants.Aggregate(label, (current, variant) => variant.ApplyToBiomeLabel(_tile, current));
        }

        Widgets.Label(rect, label.CapitalizeFirst());
    }

    private static Rect DoBiomeDescription(Listing_Standard listingStandard, string description, float float0 = -1f, string str0 = null)
    {
        if (_tile.BiomeVariants != null)
        {
            description = _tile.BiomeVariants.Aggregate(description, (current, variant) => variant.ApplyToBiomeDescription(_tile, current));
        }

        return listingStandard.Label(description);
    }

    private static void DoSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        if (_tile.Landforms != null)
        {
            var mainLandform = _tile.Landforms.LastOrDefault(l => !l.IsLayer);
            if (mainLandform != null)
            {
                var lfStr = mainLandform.TranslatedNameWithDirection(_tile.LandformDirection).CapitalizeFirst();
                listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Landform".Translate(), lfStr);
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

        if (Find.World.HasCaves(_tileId))
        {
            sb.AppendWithComma("HasCaves".Translate());
        }

        if (sb.Length > 0)
            listingStandard.LabelDouble("SpecialFeatures".Translate(), sb.ToString().CapitalizeFirst());

        listingStandard.Gap();

        GeologicalLandformsAPI.RunOnTerrainTab(listingStandard);
    }
}

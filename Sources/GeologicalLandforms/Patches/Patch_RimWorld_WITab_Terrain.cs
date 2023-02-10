using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private static readonly MethodInfo Method_AppendWithComma = AccessTools.Method(typeof(GenText), "AppendWithComma");
    private static readonly MethodInfo Method_Label = AccessTools.Method(typeof(Listing_Standard), "Label", new[] { typeof(string), typeof(float), typeof(string) });
    private static readonly MethodInfo Method_WLabel = AccessTools.Method(typeof(Widgets), "Label", new[] { typeof(Rect), typeof(TaggedString) });
    private static readonly MethodInfo Method_LabelDouble = AccessTools.Method(typeof(Listing_Standard), "LabelDouble", new[] { typeof(string), typeof(string), typeof(string) });
    private static readonly MethodInfo Method_GetBiomeLabel = AccessTools.Method(typeof(Patch_RimWorld_WITab_Terrain), nameof(GetBiomeLabel));
    private static readonly MethodInfo Method_GetBiomeDescription = AccessTools.Method(typeof(Patch_RimWorld_WITab_Terrain), nameof(GetBiomeDescription));
    private static readonly MethodInfo Method_GetSpecialFeatures = AccessTools.Method(typeof(Patch_RimWorld_WITab_Terrain), nameof(GetSpecialFeatures));

    private static int _tileId;
    private static WorldTileInfo _tile;

    [HarmonyPrefix]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static void FillTabPrefix()
    {
        _tileId = Find.WorldSelector.selectedTile;
        _tile = WorldTileInfo.Get(_tileId);
    }

    [HarmonyTranspiler]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillTab(IEnumerable<CodeInstruction> instructions)
    {
        var foundAWC = false;
        var patchedBL = false;
        var patchedBD = false;
        var patchedSF = false;

        foreach (var instruction in instructions)
        {
            if (!foundAWC && !patchedBL)
            {
                if (instruction.Calls(Method_WLabel))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method_GetBiomeLabel);
                    patchedBL = true;
                    continue;
                }
            }

            if (!foundAWC && !patchedBD)
            {
                if (instruction.Calls(Method_Label))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method_GetBiomeDescription);
                    patchedBD = true;
                    continue;
                }
            }

            if (foundAWC && !patchedSF)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                    continue;
                }

                if (instruction.Calls(Method_LabelDouble))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method_GetSpecialFeatures);
                    patchedSF = true;
                    continue;
                }
            }

            if (instruction.Calls(Method_AppendWithComma))
            {
                foundAWC = true;
            }

            yield return instruction;
        }

        if (patchedBL == false)
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_WITab_Terrain biome label");
        if (patchedBD == false)
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_WITab_Terrain biome description");
        if (patchedSF == false)
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_WITab_Terrain special features");
    }

    private static void GetBiomeLabel(Rect rect, TaggedString labelBase)
    {
        var label = labelBase.Resolve().UncapitalizeFirst();

        if (_tile.BiomeVariants != null)
        {
            label = _tile.BiomeVariants.Aggregate(label, (current, variant) => variant.ApplyToBiomeLabel(_tile, current));
        }

        Widgets.Label(rect, label.CapitalizeFirst());
    }

    private static Rect GetBiomeDescription(Listing_Standard listingStandard, string description, float float0 = -1f, string str0 = null)
    {
        if (_tile.BiomeVariants != null)
        {
            description = _tile.BiomeVariants.Aggregate(description, (current, variant) => variant.ApplyToBiomeDescription(_tile, current));
        }

        return listingStandard.Label(description);
    }

    private static void GetSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
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

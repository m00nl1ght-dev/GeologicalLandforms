using System.Collections.Generic;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class LabelUtils
{
    public static readonly Texture2D DismissIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss");

    private const string OwnPackageId = "m00nl1ght.GeologicalLandforms";

    private static readonly List<string> LabelBuffer = [];

    public static string LabelForLandform(Landform landform)
    {
        var label = landform.TranslatedNameForSelection.CapitalizeFirst();

        if (landform.Manifest.IsCustom)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Custom".Translate());
        if (landform.Manifest.IsExperimental)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Experimental".Translate());
        if (landform.ModContentPack != null && landform.ModContentPack.PackageId != OwnPackageId)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(landform.ModContentPack.Name));
        if (landform.WorldTileReq.Commonness <= 0)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ZeroCommonness".Translate());
        if (landform.WorldTileReq is { Topology: Topology.CliffOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCliff".Translate());
        if (landform.WorldTileReq is { Topology: Topology.CoastOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCoast".Translate());

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    public static string LabelForBiome(BiomeDef biome, bool preconfigured)
    {
        var mcp = biome.modContentPack;
        var label = biome.label.CapitalizeFirst();

        if (mcp is { IsOfficialMod: false } && mcp.PackageId != OwnPackageId)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(mcp.Name));

        if (preconfigured)
            LabelBuffer.Add("GeologicalLandforms.Settings.BiomeConfig.ExcludedByAuthor".Translate());

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    public static string LabelForBiomeVariant(BiomeVariantDef biomeVariant)
    {
        var mcp = biomeVariant.modContentPack;
        var label = biomeVariant.label.CapitalizeFirst();

        if (mcp is { IsOfficialMod: false } && mcp.PackageId != OwnPackageId)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(mcp.Name));

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }
}

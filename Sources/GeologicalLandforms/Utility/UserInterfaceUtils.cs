using System.Collections.Generic;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class UserInterfaceUtils
{
    public static readonly Texture2D IconDismiss = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss");

    #if RW_1_5_OR_GREATER
    public static Texture2D IconDelete => TexButton.Delete;
    #else
    public static Texture2D IconDelete => TexButton.DeleteX;
    #endif

    private const string OwnPackageId = "m00nl1ght.geologicallandforms";

    private static readonly List<string> LabelBuffer = [];

    public static string LabelForLandform(Landform landform)
    {
        var label = landform.TranslatedNameForSelection.CapitalizeFirst();

        if (landform.Manifest.IsCustom)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Custom".Translate());
        if (landform.Manifest.IsExperimental)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Experimental".Translate());
        if (landform.WorldTileReq.Commonness <= 0)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ZeroCommonness".Translate());

        #if RW_1_6_OR_GREATER

        if (TileMutatorsCustomization.Exclusions.TryGetValue(landform.Id, out var exclusions) && exclusions.Length > 0)
        {
            var def = DefDatabase<TileMutatorDef>.GetNamedSilentFail(exclusions[0]);
            if (def is { modContentPack: not null })
            {
                LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesMutator"
                    .Translate(def.LabelCap, def.modContentPack.ContentSourceLabel()));
            }
        }

        if (TileMutatorsCustomization.MadeObsoleteBy.TryGetValue(landform.Id, out var madeObsoleteBy))
        {
            var def = DefDatabase<TileMutatorDef>.GetNamedSilentFail(madeObsoleteBy);
            if (def is { modContentPack: not null } && !TileMutatorsCustomization.IsTileMutatorDisabled(def))
            {
                LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.MadeObsoleteByMutator"
                    .Translate(def.LabelCap, def.modContentPack.ContentSourceLabel()));
            }
        }

        #else

        if (landform.ModContentPack != null && landform.ModContentPack.PackageId != OwnPackageId)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(landform.ModContentPack.Name));
        if (landform.WorldTileReq is { Topology: Topology.CliffOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCliff".Translate());
        if (landform.WorldTileReq is { Topology: Topology.CoastOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCoast".Translate());
        if (landform.WorldTileReq is { Commonness: >= 1f } && landform.LayerConfig?.LayerId == "river" &&
            (landform.WorldTileReq.AllowedRiverTypes?.Contains(RiverType.Normal) ?? false))
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaRiver".Translate());

        #endif

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    #if RW_1_6_OR_GREATER

    private static readonly Dictionary<string, string> MutatorSuffixes = new()
    {
        { "AbandonedColonyTribal", "(tribal)" },
        { "AbandonedColonyOutlander", "(outlander)" },
        { "WildTropicalPlants", "(tropical)" },
        { "LakeWithIslands", "(multiple)" },
        { "LavaCrater", "(crater)" },
        { "Cove", "(cove)" },
    };

    public static string LabelForTileMutator(TileMutatorDef def, bool extraInfo)
    {
        if (MutatorSuffixes.TryGetValue(def.defName, out var suffix))
        {
            return def.LabelCap + " " + suffix;
        }

        return def.LabelCap;
    }

    #endif

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

using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.GUI;
using LunarFramework.Utility;
using MapPreview;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class GeologicalLandformsSettings : LunarModSettings
{
    public readonly Entry<int> MaxLandformSearchRadius = MakeEntry(100);
    public readonly Entry<float> AnimalDensityFactorForSecludedAreas = MakeEntry(0.5f);

    public readonly Entry<bool> EnableCellFinderOptimization = MakeEntry(true);
    public readonly Entry<bool> EnableLandformScaling = MakeEntry(true);
    public readonly Entry<bool> EnableExperimentalLandforms = MakeEntry(false);
    public readonly Entry<bool> EnableGodMode = MakeEntry(false);

    public readonly Entry<bool> IgnoreWorldTileReqInGodMode = MakeEntry(false);
    public readonly Entry<bool> ShowWorldTileDebugInfo = MakeEntry(false);
    public readonly Entry<bool> EnableMapDebugPawnCommands = MakeEntry(false);
    public readonly Entry<bool> UnidirectionalBiomeTransitions = MakeEntry(false);
    public readonly Entry<bool> DisableBiomeTransitionPostProcessing = MakeEntry(false);

    public readonly Entry<List<string>> DisabledLandforms = MakeEntry(new List<string>());
    public readonly Entry<List<string>> DisabledBiomeVariants = MakeEntry(new List<string>());

    public readonly Entry<List<string>> BiomesExcludedFromLandforms = MakeEntry(new List<string>());
    public readonly Entry<List<string>> BiomesExcludedFromTransitions = MakeEntry(new List<string>());

    protected override string TranslationKeyPrefix => "GeologicalLandforms.Settings";

    public GeologicalLandformsSettings() : base(GeologicalLandformsMod.LunarAPI)
    {
        MakeTab("Tab.General", DoGeneralSettingsTab);
        MakeTab("Tab.Landforms", DoLandformsSettingsTab, () => LandformManager.Landforms.Count > 0);
        MakeTab("Tab.BiomeConfig", DoBiomeConfigSettingsTab, () => LandformManager.Landforms.Count > 0);
        MakeTab("Tab.BiomeVariants", DoBiomeVariantsSettingsTab, () => DefDatabase<BiomeVariantDef>.DefCount > 0);
        MakeTab("Tab.Debug", DoDebugSettingsTab, () => Prefs.DevMode);
    }

    private void DoGeneralSettingsTab(LayoutRect layout)
    {
        layout.PushEnabled(!MapPreviewAPI.IsGeneratingPreview);
        
        if (LunarGUI.Button(layout, Label("OpenEditor")))
        {
            Find.WindowStack.Add(new LandformGraphEditor());

            var worldSelector = Find.World?.UI?.selector;
            if (worldSelector is { selectedTile: > 0 })
            {
                var tileInfo = WorldTileInfo.Get(worldSelector.selectedTile);
                var landform = tileInfo.Landforms?.FirstOrDefault(lf => !lf.IsLayer);
                if (landform != null) LandformGraphEditor.ActiveEditor?.OpenLandform(landform);
            }
        }

        if (LunarGUI.Button(layout, Label("OpenDataDir")))
        {
            Application.OpenURL(LandformManager.CustomLandformsDir(LandformManager.CurrentVersion));
        }

        if (LunarGUI.Button(layout, Label("ResetAll")))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(Label("ConfirmResetAll"), ResetAll));
        }

        layout.PopEnabled();

        layout.Abs(10f);

        LunarGUI.LabelDouble(layout, Label("MaxLandformSearchRadius"), MaxLandformSearchRadius.Value.ToString("F0"));
        LunarGUI.Slider(layout, ref MaxLandformSearchRadius.Value, 10, 500);

        LunarGUI.LabelDouble(layout, Label("AnimalDensityFactorForSecludedAreas"), AnimalDensityFactorForSecludedAreas.Value.ToString("F2"));
        LunarGUI.Slider(layout, ref AnimalDensityFactorForSecludedAreas.Value, 0.25f, 0.75f);

        layout.Abs(10f);

        layout.PushChanged();

        if (LandformManager.Landforms.Values.Any(lf => lf.Manifest.IsExperimental))
        {
            LunarGUI.Checkbox(layout, ref EnableExperimentalLandforms.Value, Label("EnableExperimentalLandforms"));
        }

        LunarGUI.Checkbox(layout, ref EnableGodMode.Value, Label("EnableGodMode"));
        LunarGUI.Checkbox(layout, ref EnableCellFinderOptimization.Value, Label("EnableCellFinderOptimization"));
        LunarGUI.Checkbox(layout, ref EnableLandformScaling.Value, Label("EnableLandformScaling"));

        if (layout.PopChanged()) MapPreviewAPI.NotifyWorldChanged();
    }

    private void DoLandformsSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();
        
        foreach (var landform in LandformManager.Landforms.Values)
        {
            if (landform.Manifest.IsExperimental && !EnableExperimentalLandforms) continue;
            if (landform.WorldTileReq == null) continue;

            LunarGUI.ToggleTableRow(layout, landform.Id, true, LabelForLandform(landform), DisabledLandforms);
        }

        if (layout.PopChanged()) MapPreviewAPI.NotifyWorldChanged();
    }

    private void DoBiomeConfigSettingsTab(LayoutRect layout)
    {
        layout.BeginAbs(Text.LineHeight, new LayoutParams { Horizontal = true, Reversed = true });
        LunarGUI.Label(layout.Abs(22f), "MB");
        layout.Abs(4f);
        LunarGUI.Label(layout.Abs(22f), "LF");
        layout.Abs(4f);
        LunarGUI.Label(layout.Abs(-1), Label("BiomeConfig.Header"));
        layout.End();

        LunarGUI.SeparatorLine(layout, 3f);

        foreach (var biome in DefDatabase<BiomeDef>.AllDefsListForReading)
        {
            var properties = biome.Properties();
            var preconfigured = !properties.allowLandforms || !properties.allowBiomeTransitions;
            var label = LabelForBiomeConfig(biome, preconfigured);

            if (preconfigured && biome.modContentPack is { IsOfficialMod: true }) continue;

            var listForLandforms = properties.allowLandforms ? BiomesExcludedFromLandforms.Value : null;
            var listForTransitions = properties.allowBiomeTransitions ? BiomesExcludedFromTransitions.Value : null;

            layout.PushChanged();

            LunarGUI.ToggleTableRow(layout, biome.defName, true, label, listForLandforms, listForTransitions);

            if (layout.PopChanged())
            {
                if (listForLandforms != null) properties.allowLandformsByUser = !listForLandforms.Contains(biome.defName);
                if (listForTransitions != null) properties.allowBiomeTransitionsByUser = !listForTransitions.Contains(biome.defName);

                MapPreviewAPI.NotifyWorldChanged();
            }
        }
    }

    private void DoBiomeVariantsSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();
        
        foreach (var biomeVariant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
        {
            LunarGUI.ToggleTableRow(layout, biomeVariant.defName, true, LabelForBiomeVariant(biomeVariant), DisabledBiomeVariants);
        }

        if (layout.PopChanged()) MapPreviewAPI.NotifyWorldChanged();
    }

    private void DoDebugSettingsTab(LayoutRect layout)
    {
        LunarGUI.Checkbox(layout, ref ShowWorldTileDebugInfo.Value, Label("Debug.ShowWorldTileDebugInfo"));
        LunarGUI.Checkbox(layout, ref EnableMapDebugPawnCommands.Value, Label("Debug.EnableMapDebugPawnCommands"));

        if (EnableGodMode)
        {
            LunarGUI.Checkbox(layout, ref IgnoreWorldTileReqInGodMode.Value, Label("Debug.IgnoreWorldTileReqInGodMode"));
        }

        LunarGUI.Checkbox(layout, ref UnidirectionalBiomeTransitions.Value, Label("Debug.UnidirectionalBiomeTransitions"));
        LunarGUI.Checkbox(layout, ref DisableBiomeTransitionPostProcessing.Value, Label("Debug.DisableBiomeTransitionPostProcessing"));

        DebugActions.DebugActionsGUI(layout);
    }

    private static readonly List<string> LabelBuffer = new();

    private string LabelForLandform(Landform landform)
    {
        var label = landform.TranslatedNameForSelection.CapitalizeFirst();

        if (landform.Manifest.IsCustom)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Custom".Translate());
        if (landform.Manifest.IsExperimental)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Experimental".Translate());
        if (landform.ModContentPack != null && landform.ModContentPack != GeologicalLandformsMod.Settings.Mod.Content)
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

    private string LabelForBiomeConfig(BiomeDef biome, bool preconfigured)
    {
        var mcp = biome.modContentPack;
        var label = biome.label.CapitalizeFirst();

        if (mcp is { IsOfficialMod: false } && mcp != GeologicalLandformsMod.Settings.Mod.Content)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(mcp.Name));

        if (preconfigured)
            LabelBuffer.Add("GeologicalLandforms.Settings.BiomeConfig.ExcludedByAuthor".Translate());

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    private string LabelForBiomeVariant(BiomeVariantDef biomeVariant)
    {
        var mcp = biomeVariant.modContentPack;
        var label = biomeVariant.label.CapitalizeFirst();

        if (mcp is { IsOfficialMod: false } && mcp != GeologicalLandformsMod.Settings.Mod.Content)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(mcp.Name));

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    public void ApplyDefEffects()
    {
        if (!EnableCellFinderOptimization) GeologicalLandformsMod.Logger.Log("CellFinder optimizations are disabled.");
        if (!EnableLandformScaling) GeologicalLandformsMod.Logger.Log("Landform scaling is disabled.");
        
        foreach (var biome in DefDatabase<BiomeDef>.AllDefsListForReading)
        {
            biome.Properties().allowLandformsByUser = !BiomesExcludedFromLandforms.Value.Contains(biome.defName);
            biome.Properties().allowBiomeTransitionsByUser = !BiomesExcludedFromTransitions.Value.Contains(biome.defName);
        }
    }

    public override void ResetAll()
    {
        base.ResetAll();
        BiomeProperties.RebuildCache();
        LandformManager.ResetAll();
        LandformManager.SaveAllEdited();
        ApplyDefEffects();
        MapPreviewAPI.NotifyWorldChanged();
    }
}

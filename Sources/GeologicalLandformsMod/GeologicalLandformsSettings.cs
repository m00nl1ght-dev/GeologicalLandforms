using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework.GUI;
using LunarFramework.Utility;
using MapPreview;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class GeologicalLandformsSettings : LunarModSettings
{
    public readonly Entry<int> MaxLandformSearchRadius = MakeEntry(200);
    public readonly Entry<float> AnimalDensityFactorForSecludedAreas = MakeEntry(0.5f);

    public readonly Entry<bool> EnableCellFinderOptimization = MakeEntry(true);
    public readonly Entry<bool> EnableLandformScaling = MakeEntry(true);
    public readonly Entry<bool> EnableExperimentalLandforms = MakeEntry(false);
    public readonly Entry<bool> EnableGodMode = MakeEntry(false);

    public readonly Entry<bool> IgnoreWorldTileReqInGodMode = MakeEntry(false);
    public readonly Entry<bool> ShowWorldTileDebugInfo = MakeEntry(false);
    public readonly Entry<bool> UnidirectionalBiomeTransitions = MakeEntry(false);
    public readonly Entry<bool> DisableBiomeTransitionPostProcessing = MakeEntry(false);

    public readonly Entry<bool> DevQuickTestOverrideEnabled = MakeEntry(false);
    public readonly Entry<int> DevQuickTestOverrideMapSize = MakeEntry(150);
    public readonly Entry<string> DevQuickTestOverrideBiome = MakeEntry("None");
    public readonly Entry<string> DevQuickTestOverrideLandform = MakeEntry("None");

    public readonly Entry<List<string>> DisabledLandforms = MakeEntry(new List<string>());
    public readonly Entry<List<string>> DisabledBiomeVariants = MakeEntry(new List<string>());

    public readonly Entry<List<string>> BiomesExcludedFromLandforms = MakeEntry(new List<string>());
    public readonly Entry<List<string>> BiomesExcludedFromTransitions = MakeEntry(new List<string>());

    protected override string TranslationKeyPrefix => "GeologicalLandforms.Settings";

    public GeologicalLandformsSettings() : base(GeologicalLandformsMod.LunarAPI)
    {
        MakeTab("Tab.General", DoGeneralSettingsTab);
        MakeTab("Tab.Landforms", DoLandformsSettingsTab, () => LandformManager.LandformsById.Count > 0);
        MakeTab("Tab.BiomeConfig", DoBiomeConfigSettingsTab, () => LandformManager.LandformsById.Count > 0);
        MakeTab("Tab.BiomeVariants", DoBiomeVariantsSettingsTab, () => DefDatabase<BiomeVariantDef>.DefCount > 0);
        MakeTab("Tab.Debug", DoDebugSettingsTab, () => Prefs.DevMode);
    }

    private void DoGeneralSettingsTab(LayoutRect layout)
    {
        layout.PushEnabled(!MapPreviewAPI.IsGeneratingPreview);

        if (LunarGUI.Button(layout, Label("OpenEditor")))
        {
            Find.WindowStack.Add(new LandformGraphEditor());
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

        if (LandformManager.LandformsById.Values.Any(lf => lf.Manifest.IsExperimental))
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

        foreach (var landform in LandformManager.LandformsById.Values)
        {
            if (landform.Manifest.IsExperimental && !EnableExperimentalLandforms) continue;
            if (landform.WorldTileReq == null) continue;
            if (landform.IsInternal) continue;

            var label = UserInterfaceUtils.LabelForLandform(landform);
            LunarGUI.ToggleTableRow(layout, landform.Id, true, label, DisabledLandforms);
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
            var label = UserInterfaceUtils.LabelForBiome(biome, preconfigured);

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
            if (biomeVariant.label.NullOrEmpty()) continue;
            var label = UserInterfaceUtils.LabelForBiomeVariant(biomeVariant);
            LunarGUI.ToggleTableRow(layout, biomeVariant.defName, true, label, DisabledBiomeVariants);
        }

        if (layout.PopChanged()) MapPreviewAPI.NotifyWorldChanged();
    }

    private void DoDebugSettingsTab(LayoutRect layout)
    {
        LunarGUI.Checkbox(layout, ref ShowWorldTileDebugInfo.Value, Label("Debug.ShowWorldTileDebugInfo"));

        if (EnableGodMode)
        {
            LunarGUI.Checkbox(layout, ref IgnoreWorldTileReqInGodMode.Value, Label("Debug.IgnoreWorldTileReqInGodMode"));
        }

        LunarGUI.Checkbox(layout, ref UnidirectionalBiomeTransitions.Value, Label("Debug.UnidirectionalBiomeTransitions"));
        LunarGUI.Checkbox(layout, ref DisableBiomeTransitionPostProcessing.Value, Label("Debug.DisableBiomeTransitionPostProcessing"));

        LunarGUI.Checkbox(layout, ref DevQuickTestOverrideEnabled.Value, Label("Debug.DevQuickTestOverrideEnabled"));

        if (DevQuickTestOverrideEnabled)
        {
            LunarGUI.SeparatorLine(layout);

            layout.BeginAbs(28f);

            LunarGUI.Label(layout.Rel(0.3f), Label("Debug.DevQuickTestOverrideLandform"));

            if (LunarGUI.Button(layout, DevQuickTestOverrideLandform))
            {
                var options = new List<FloatMenuOption>
                {
                    new("None".Translate(), () => DevQuickTestOverrideLandform.Value = "None")
                };

                options.AddRange(LandformManager.LandformsById.Values.Where(e => !e.IsInternal).OrderBy(e => e.Id)
                    .Select(e => new FloatMenuOption(e.Id, () => DevQuickTestOverrideLandform.Value = e.Id)));

                Find.WindowStack.Add(new FloatMenu(options));
            }

            layout.End();

            layout.BeginAbs(28f);

            LunarGUI.Label(layout.Rel(0.3f), Label("Debug.DevQuickTestOverrideBiome"));

            if (LunarGUI.Button(layout, DevQuickTestOverrideBiome))
            {
                var options = new List<FloatMenuOption>
                {
                    new("None".Translate(), () => DevQuickTestOverrideBiome.Value = "None")
                };

                options.AddRange(DefDatabase<BiomeDef>.AllDefs.OrderBy(e => e.defName)
                    .Select(e => new FloatMenuOption(e.defName, () => DevQuickTestOverrideBiome.Value = e.defName)));

                Find.WindowStack.Add(new FloatMenu(options));
            }

            layout.End();

            layout.BeginAbs(28f);

            LunarGUI.Label(layout.Rel(0.3f), Label("Debug.DevQuickTestOverrideMapSize"));

            if (LunarGUI.Button(layout, DevQuickTestOverrideMapSize.Value + " x " + DevQuickTestOverrideMapSize.Value))
            {
                var options = new List<FloatMenuOption>
                {
                    new("25 x 25", () => DevQuickTestOverrideMapSize.Value = 25),
                    new("50 x 50", () => DevQuickTestOverrideMapSize.Value = 50),
                    new("75 x 75", () => DevQuickTestOverrideMapSize.Value = 75),
                    new("100 x 100", () => DevQuickTestOverrideMapSize.Value = 100),
                    new("150 x 150", () => DevQuickTestOverrideMapSize.Value = 150),
                    new("200 x 200", () => DevQuickTestOverrideMapSize.Value = 200),
                    new("250 x 250", () => DevQuickTestOverrideMapSize.Value = 250),
                    new("300 x 300", () => DevQuickTestOverrideMapSize.Value = 300),
                    new("400 x 400", () => DevQuickTestOverrideMapSize.Value = 400)
                };

                Find.WindowStack.Add(new FloatMenu(options));
            }

            layout.End();

            LunarGUI.SeparatorLine(layout);
        }

        DebugActions.DebugActionsGUI(layout);
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

using System;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using NodeEditorFramework.Utilities;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

[LunarComponentEntrypoint]
public class GeologicalLandformsMod : Mod
{
    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Geological Landforms Mod", Init, Cleanup);

    internal static LogContext Logger => LunarAPI.LogContext;

    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;

    internal static GeologicalLandformsSettings Settings;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(GeologicalLandformsMod).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        GeologicalLandformsAPI.LandformEnabled.AddModifier(0, IsLandformEnabled);
        GeologicalLandformsAPI.BiomeVariantEnabled.AddModifier(0, IsBiomeVariantEnabled);
        GeologicalLandformsAPI.TerrainTabUI.AddObserver(0, TerrainTabUI.DoTerrainTabUI);
        GeologicalLandformsAPI.LandformGridSize.AddModifier(0, GridSizeProvider);
        GeologicalLandformsAPI.AnimalDensityFactor.AddModifier(0, AnimalDensityFactorForMap);
        GeologicalLandformsAPI.CellFinderOptimizationEnabled.AddSupplier(0, () => Settings.EnableCellFinderOptimization.Value);
        GeologicalLandformsAPI.VanillaMountainGenerationEnabled.AddSupplier(0, () => Settings.DisabledLandforms.Value.Contains("Cliff"));
        BiomeTransition.UnidirectionalBiomeTransitions.AddSupplier(0, () => Settings.UnidirectionalBiomeTransitions.Value);
        BiomeTransition.PostProcessBiomeTransitions.AddSupplier(0, () => !Settings.DisableBiomeTransitionPostProcessing.Value);

        Settings.ApplyDefEffects();

        var modContentPack = LunarAPI.Component.LatestVersionProvidedBy.ModContentPack;
        var assetBundle = modContentPack.assetBundles.loadedAssetBundles.Find(b => b.name == "terraingraph");
        if (assetBundle == null) throw new Exception("terraingraph asset bundle is missing");

        ResourceManager.InitAssetBundle(assetBundle);

        #if DEBUG
        DebugActions.SetupDebugActions();
        #endif
    }

    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }

    private static int GridSizeProvider(int baseValue)
    {
        return Settings.EnableLandformScaling ? baseValue : Landform.GeneratingMapSizeMin;
    }

    private static float AnimalDensityFactorForMap(BiomeGrid biomeGrid, float baseValue)
    {
        var primaryBiome = biomeGrid?.Primary.Biome;
        if (primaryBiome == null || !primaryBiome.Properties().AllowLandforms) return baseValue;
        var openGroundFraction = biomeGrid.OpenGroundFraction;
        var scaleFactor = 2f - Settings.AnimalDensityFactorForSecludedAreas * 2f;
        return 1f + (openGroundFraction - 1f) * scaleFactor;
    }

    public static bool IsLandformEnabled(Landform landform, bool baseValue = true)
    {
        if (!Settings.EnableExperimentalLandforms && landform.Manifest.IsExperimental) return false;
        if (Settings.DisabledLandforms.Value.Contains(landform.Id)) return false;
        return baseValue;
    }

    public static bool IsBiomeVariantEnabled(BiomeVariantDef biomeVariant, bool baseValue = true)
    {
        if (Settings.DisabledBiomeVariants.Value.Contains(biomeVariant.defName)) return false;
        return baseValue;
    }

    public GeologicalLandformsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<GeologicalLandformsSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Geological Landforms";
}

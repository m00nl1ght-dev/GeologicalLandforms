using System;
using System.Linq;
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
        
        GeologicalLandformsAPI.WorldTileInfoHook += WorldTileInfoHook;
        GeologicalLandformsAPI.OnTerrainTab += TerrainTabUI.DoTerrainTabUI;
        GeologicalLandformsAPI.PutLandformGridSizeFunction(GridSizeProvider);
        GeologicalLandformsAPI.PutAnimalDensityFactorFunction(AnimalDensityFactorForMap);
        GeologicalLandformsAPI.PutCellFinderOptimizationFilter(_ => Settings.EnableCellFinderOptimization);

        var modContentPack = LunarAPI.Component.LatestVersionProvidedBy.ModContentPack;
        var assetBundle = modContentPack.assetBundles.loadedAssetBundles.Find(b => b.name == "terraingraph");
        if (assetBundle == null) throw new Exception("terraingraph asset bundle is missing");
        
        ResourceManager.InitAssetBundle(assetBundle);
    }
    
    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }
    
    private static void WorldTileInfoHook(WorldTileInfoPrimer info)
    {
        info.Landforms = info.Landforms?.Where(IsLandformEnabled).ToList();
    }

    private static int GridSizeProvider()
    {
        return Settings.EnableLandformScaling ? Landform.DefaultGridFullSize : Landform.GeneratingMapSizeMin;
    }

    private static float AnimalDensityFactorForMap(BiomeGrid biomeGrid)
    {
        var primaryBiome = biomeGrid?.Primary.Biome;
        if (primaryBiome == null || !primaryBiome.Properties().allowLandforms) return 1f;
        var openGroundFraction = biomeGrid.OpenGroundFraction;
        var scaleFactor = 2f - Settings.AnimalDensityFactorForSecludedAreas * 2f;
        return 1f + (openGroundFraction - 1f) * scaleFactor;
    }

    public static bool IsLandformEnabled(Landform landform)
    {
        if (!Settings.EnableExperimentalLandforms && landform.Manifest.IsExperimental) return false;
        return true;
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
using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework.Utilities;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class InitOnStartup
{
    static InitOnStartup()
    {
        EventHooks.OnTerrainTab += TerrainTabUI.DoTerrainTabUI;
        EventHooks.PutCellFinderOptimizationFilter(_ => ModInstance.Settings.EnableCellFinderOptimization);
        EventHooks.PutLandformGridSizeFunction(() => ModInstance.Settings.EnableLandformScaling ? Landform.DefaultGridFullSize : Landform.GeneratingMapSizeMin);
        EventHooks.PutAnimalDensityFactorFunction(AnimalDensityFactorForMap);
        
        var assetBundle = ModInstance.ModContentPack.assetBundles.loadedAssetBundles.Find(b => b.name == "terraingraph");
        if (assetBundle == null) throw new Exception("terraingraph asset bundle is missing");
        ResourceManager.InitAssetBundle(assetBundle);
    }
    
    public static float AnimalDensityFactorForMap(BiomeGrid biomeGrid)
    {
        var openGroundFraction = biomeGrid?.OpenGroundFraction ?? 1f;
        var scaleFactor = 2f - ModInstance.Settings.AnimalDensityFactorForSecludedAreas * 2f;
        return 1f + (openGroundFraction - 1f) * scaleFactor;
    }
}
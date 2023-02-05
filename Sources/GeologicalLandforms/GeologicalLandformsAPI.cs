using System;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.XML;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using LunarFramework.Utility;
using MapPreview;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms;

[LunarComponentEntrypoint]
public static class GeologicalLandformsAPI
{
    // ### Init ###

    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Geological Landforms", Init, Cleanup);

    internal static LogContext Logger => LunarAPI.LogContext;

    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;

    static GeologicalLandformsAPI()
    {
        typeof(XmlDynamicValueSetup).RunClassConstructor();
        GenTypes.IgnoredNamespaceNames.AddDistinct("GeologicalLandforms.Defs");
    }

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(GeologicalLandformsAPI).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        MapPreviewAPI.OnWorldChanged += WorldTileInfo.InvalidateCache;
        MapPreviewAPI.AddStableSeedCondition(map => WorldTileInfo.Get(map.Tile).HasLandforms);

        ReflectionUtility.AddSearchableAssembly(typeof(GeologicalLandformsAPI).Assembly);
        ReflectionUtility.AddSearchableAssembly(typeof(TerrainCanvas).Assembly);

        ReflectionUtility.AddIdentifierReplacement("0_TerrainGraph", "TerrainGraph");
        ReflectionUtility.AddIdentifierReplacement("1_GeologicalLandforms", "GeologicalLandforms");

        NodeEditor.ReInit(false);

        BiomeWorkerConfig.ApplyAll();
        BiomeProperties.RebuildCache();
        BiomeVariantDef.InitialLoad();

        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
    }

    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }

    // ### Public API ###

    public static Func<bool> DisableVanillaMountainGeneration { get; set; } = () => false;

    public static Func<bool> UseCellFinderOptimization { get; set; } = () => true;
    
    public static Func<int> LandformGridSize { get; set; } = () => Landform.DefaultGridFullSize;
    
    public static Func<BiomeGrid, float> AnimalDensityFactor { get; set; } = _ => 1f;
    
    public static Func<bool> UnidirectionalBiomeTransitions = () => false;
    
    public static Func<bool> PostProcessBiomeTransitions = () => true;

    public static event Action<BiomeDef, BiomeProperties> BiomePropertiesHook;

    public static BiomeProperties ApplyBiomePropertiesHook(BiomeDef biome, BiomeProperties properties)
    {
        if (BiomePropertiesHook == null) return properties;
        properties = new BiomeProperties(properties);
        BiomePropertiesHook.Invoke(biome, properties);
        return properties;
    }

    public static event Action<WorldTileInfoPrimer> WorldTileInfoHook;

    public static void ApplyWorldTileInfoHook(WorldTileInfoPrimer worldTileInfo)
    {
        WorldTileInfoHook?.Invoke(worldTileInfo);
    }

    public static event Action<Listing_Standard> OnTerrainTab;

    public static void RunOnTerrainTab(Listing_Standard listing)
    {
        OnTerrainTab?.Invoke(listing);
    }
}

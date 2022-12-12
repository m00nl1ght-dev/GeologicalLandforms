using System;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
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
        
        ExtensionUtils.Init();
        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
        BiomeVariantDef.InitialLoad();

        /*
        bool done = false;
        LunarAPI.LifecycleHooks.DoOnceOnMainMenu(() =>
        {
            LunarAPI.LifecycleHooks.DoOnGUI(() =>
            {
                if (done) return;
                var landformGraphEditor = new LandformGraphEditor();
                Find.WindowStack.Add(landformGraphEditor);
                landformGraphEditor.OpenLandform(LandformManager.FindById("SurfaceCaves"));
                done = true;
            });
        });
        */
    }
    
    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }
    
    // ### Public API ###

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

    public static Func<Map, bool> CellFinderOptimizationFilter { get; private set; } = _ => true;
    public static void PutCellFinderOptimizationFilter(Func<Map, bool> filter)
    {
        CellFinderOptimizationFilter = filter ?? (_ => true);
    }
    
    public static Func<int> LandformGridSizeFunction { get; private set; } = () => Landform.DefaultGridFullSize;
    public static void PutLandformGridSizeFunction(Func<int> function)
    {
        LandformGridSizeFunction = function ?? (() => Landform.DefaultGridFullSize);
    }

    public static Func<BiomeGrid, float> AnimalDensityFactorFunction { get; private set; } = _ => 1f;
    public static void PutAnimalDensityFactorFunction(Func<BiomeGrid, float> function)
    {
        AnimalDensityFactorFunction = function ?? (_ => 1f);
    }
}
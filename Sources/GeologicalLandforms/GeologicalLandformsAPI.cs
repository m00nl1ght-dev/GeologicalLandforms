using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using LunarFramework.Utility;
using MapPreview;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using TerrainGraph.Util;
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
        GenTypes.IgnoredNamespaceNames.AddUnique("GeologicalLandforms.Defs");
    }

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(GeologicalLandformsAPI).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        MainPatchGroup.CheckForConflicts(Logger);

        MapPreviewAPI.OnWorldChanged += WorldTileInfo.InvalidateCache;
        MapPreviewAPI.AddStableSeedCondition(map => WorldTileInfo.Get(map.Tile).HasLandforms());

        ReflectionUtility.AddSearchableAssembly(typeof(GeologicalLandformsAPI).Assembly);
        ReflectionUtility.AddSearchableAssembly(typeof(TerrainCanvas).Assembly);

        ReflectionUtility.AddIdentifierReplacement("0_TerrainGraph", "TerrainGraph");
        ReflectionUtility.AddIdentifierReplacement("1_GeologicalLandforms", "GeologicalLandforms");

        NodeEditor.ReInit(false);

        BiomeWorkerConfig.ApplyAll();
        BiomeProperties.RebuildCache();
        BiomeVariantDef.InitialLoad();

        WorldGenStep_Landforms.Register();

        #if RW_1_6_OR_GREATER
        PlanetLayerDefOf.Surface.worldDrawLayers.Add(typeof(WorldLayer_Landforms));
        PlanetLayerDefOf.Surface.worldDrawLayers.Add(typeof(WorldLayer_BiomeTransitions));
        #endif

        LandformGraphEditor.InitialSetup();
        LandformManager.InitialLoad();
    }

    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }

    // ### Public API ###

    public static readonly ExtensionPoint<Tile, bool> VanillaMountainGenerationEnabled = new(true);

    public static readonly ExtensionPoint<Map, bool> CellFinderOptimizationEnabled = new(true);

    public static readonly ExtensionPoint<IntVec2, int> LandformGridSize = new(Landform.DefaultGridFullSize);

    public static readonly ExtensionPoint<BiomeGrid, float> AnimalDensityFactor = new(1f);

    public static readonly ExtensionPoint<Landform, bool> LandformEnabled = new(true);

    #if RW_1_6_OR_GREATER
    public static readonly ExtensionPoint<TileMutatorDef, bool> TileMutatorEnabled = new(true);
    #endif

    public static readonly ExtensionPoint<BiomeVariantDef, bool> BiomeVariantEnabled = new(true);

    public static readonly ExtensionPoint<World, WorldTileInfoPrimer> WorldTileInfoHook = new();

    public static readonly ExtensionPoint<Listing_Standard, object> TerrainTabUI = new();
}

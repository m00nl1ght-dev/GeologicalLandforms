using System;
using System.Reflection;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using HarmonyLib;
using NodeEditorFramework;
using Verse;

namespace GeologicalLandforms.ModCompat;

[StaticConstructorOnStartup]
internal static class ModCompat_MapReroll
{
    static ModCompat_MapReroll()
    {
        try
        {
            Type mapGen = GenTypes.GetTypeInAnyAssembly("MapReroll.MapPreviewGenerator");
            if (mapGen != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for MapReroll.");
                Harmony harmony = new("Geological Landforms MapReroll Compat");
                
                Type mapPreviews = GenTypes.GetTypeInAnyAssembly("MapReroll.UI.Dialog_MapPreviews");

                MethodInfo methodGenerate = AccessTools.Method(mapGen, "GeneratePreviewForSeed");
                MethodInfo methodTerrainFrom = AccessTools.Method(mapGen, "TerrainFrom");
                MethodInfo methodSetupTabs = AccessTools.Method(mapPreviews, "SetUpTabs");
                MethodInfo methodPreClose = AccessTools.Method(mapPreviews, "PreClose");

                Type self = typeof(ModCompat_MapReroll);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod methodGeneratePrefix = new(self.GetMethod(nameof(GeneratePreviewForSeed_Prefix), bindingFlags), -10);
                HarmonyMethod methodGeneratePostfix = new(self.GetMethod(nameof(GeneratePreviewForSeed_Postfix), bindingFlags), 1000);
                HarmonyMethod methodTerrainFromPrefix = new(self.GetMethod(nameof(TerrainFrom_Prefix), bindingFlags), -10);
                HarmonyMethod methodSetupTabsPostfix = new(self.GetMethod(nameof(Dialog_MapPreviews_SetUpTabs_Postfix), bindingFlags));
                HarmonyMethod methodPreClosePostfix = new(self.GetMethod(nameof(Dialog_MapPreviews_PreClose_Postfix), bindingFlags));

                harmony.Patch(methodGenerate, methodGeneratePrefix, methodGeneratePostfix);
                harmony.Patch(methodTerrainFrom, methodTerrainFromPrefix);
                harmony.Patch(methodSetupTabs, null, methodSetupTabsPostfix);
                harmony.Patch(methodPreClose, null, methodPreClosePostfix);
            }
        }
        catch
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for MapReroll!");
        }
    }
    
    private static void GeneratePreviewForSeed_Prefix(string seed, int mapTile, int mapSize)
    {
        int landformSeed = GenText.StableStringHash(seed) ^ mapTile;
        Landform.PrepareMapGen(mapSize, mapTile, landformSeed);
        RimWorld_TerrainPatchMaker.Reset();
        RimWorld_GenStep_Terrain.Init();
    }
    
    private static void GeneratePreviewForSeed_Postfix(string seed, int mapTile, int mapSize)
    {
        RimWorld_GenStep_Terrain.CleanUp();
        RimWorld_TerrainPatchMaker.Reset();
        Landform.CleanUp();
    }

    private static Landform _lastEditedLandform;
    private static NodeEditorState _lastEditorState;
    public static bool IsPreviewWindowOpen;
    
    private static void Dialog_MapPreviews_SetUpTabs_Postfix()
    {
        IsPreviewWindowOpen = true;
        
        LandformGraphEditor editor = LandformGraphEditor.ActiveEditor;
        if (editor is { HasLoadedLandform: true })
        {
            _lastEditedLandform = editor.Landform;
            _lastEditorState = editor.EditorState;
            editor.CloseLandform();
        }
    }
    
    private static void Dialog_MapPreviews_PreClose_Postfix()
    {
        LandformGraphEditor editor = LandformGraphEditor.ActiveEditor;
        
        if (editor != null && _lastEditedLandform != null && Landform.GeneratingTile == null)
        {
            editor.OpenLandform(_lastEditedLandform, _lastEditorState);
        }
        
        _lastEditedLandform = null;
        _lastEditorState = null;
        IsPreviewWindowOpen = false;
    }
    
    private static bool TerrainFrom_Prefix(ref TerrainDef __result, IntVec3 c, Map map, 
        float elevation, float fertility, MulticastDelegate riverTerrainAt, bool preferSolid)
    {
        if (RimWorld_GenStep_Terrain.UseVanillaTerrain) return true;

        var tRiver = (TerrainDef) riverTerrainAt?.DynamicInvoke(c, true);
        __result = RimWorld_GenStep_Terrain.TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }
}
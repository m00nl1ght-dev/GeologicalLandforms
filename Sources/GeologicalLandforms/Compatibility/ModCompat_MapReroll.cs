using System;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Patching;
using NodeEditorFramework;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_MapReroll : ModCompat
{
    public override string TargetAssemblyName => "MapReroll";
    public override string DisplayName => "Map Reroll";
    
    public static bool IsPreviewWindowOpen;
    
    private static Landform _lastEditedLandform;
    private static NodeEditorState _lastEditorState;
    
    [HarmonyPrefix]
    [HarmonyPriority(1000)]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Prefix(string seed, int mapTile, int mapSize)
    {
        int landformSeed = GenText.StableStringHash(seed) ^ mapTile;
        Landform.PrepareMapGen(new IntVec2(mapSize, mapSize), mapTile, landformSeed);
        RimWorld_GenStep_Terrain.Init();
    }
    
    [HarmonyPostfix]
    [HarmonyPriority(-10)]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Postfix()
    {
        RimWorld_GenStep_Terrain.CleanUp();
        Landform.CleanUp();
    }
    
    [HarmonyPrefix]
    [HarmonyPriority(1000)]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "TerrainFrom")]
    private static bool MapPreviewGenerator_TerrainFrom(ref TerrainDef __result, IntVec3 c, Map map, 
        float elevation, float fertility, MulticastDelegate riverTerrainAt, bool preferSolid)
    {
        if (RimWorld_GenStep_Terrain.UseVanillaTerrain) return true;

        var tRiver = (TerrainDef) riverTerrainAt?.DynamicInvoke(c, true);
        __result = RimWorld_GenStep_Terrain.TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);
        
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("MapReroll.UI.Dialog_MapPreviews", "SetUpTabs")]
    private static void Dialog_MapPreviews_SetUpTabs()
    {
        IsPreviewWindowOpen = true;
        
        var editor = LandformGraphEditor.ActiveEditor;
        if (editor is { HasLoadedLandform: true })
        {
            _lastEditedLandform = editor.Landform;
            _lastEditorState = editor.EditorState;
            
            editor.CloseLandform();
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("MapReroll.UI.Dialog_MapPreviews", "PreClose")]
    private static void Dialog_MapPreviews_PreClose()
    {
        var editor = LandformGraphEditor.ActiveEditor;
        
        if (editor != null && _lastEditedLandform != null && Landform.GeneratingTile == null)
        {
            editor.OpenLandform(_lastEditedLandform, _lastEditorState);
        }
        
        _lastEditedLandform = null;
        _lastEditorState = null;
        
        IsPreviewWindowOpen = false;
    }
}
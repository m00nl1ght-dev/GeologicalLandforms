using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using NodeEditorFramework;
using UnityEngine;
using Verse;

#if !RW_1_6_OR_GREATER
using GeologicalLandforms.Patches;
using System;
#endif

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_MapReroll : ModCompat
{
    public override string TargetAssemblyName => "MapReroll";
    public override string DisplayName => "Map Reroll";

    public static bool IsPreviewWindowOpen;

    private static Landform _lastEditedLandform;
    private static NodeEditorState _lastEditorState;

    [HarmonyPostfix]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "CreateMapStub")]
    private static void MapPreviewGenerator_CreateMapStub_Postfix(Map __result)
    {
        var biomeGrid = new BiomeGrid(__result);
        __result.components.Add(biomeGrid);

        Landform.Prepare(__result);

        biomeGrid.Primary.Set(__result.Biome);
        biomeGrid.Primary.Refresh(null);

        #if !RW_1_6_OR_GREATER
        Patch_RimWorld_GenStep_Terrain.Init(__result);
        #endif
    }

    [HarmonyFinalizer]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Finalizer()
    {
        #if !RW_1_6_OR_GREATER
        Patch_RimWorld_GenStep_Terrain.CleanUp();
        #endif

        Landform.CleanUp();
    }

    #if !RW_1_6_OR_GREATER

    [HarmonyPrefix]
    [HarmonyPriority(1000)]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "TerrainFrom")]
    private static bool MapPreviewGenerator_TerrainFrom(
        ref TerrainDef __result, IntVec3 c, Map map, float elevation,
        float fertility, MulticastDelegate riverTerrainAt, bool preferSolid)
    {
        if (Patch_RimWorld_GenStep_Terrain.UseVanillaTerrain) return true;

        var tRiver = (TerrainDef) riverTerrainAt?.DynamicInvoke(c, true);
        __result = Patch_RimWorld_GenStep_Terrain.TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);

        return false;
    }

    #endif

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
        else if (Prefs.DevMode && Input.GetKey(KeyCode.LeftShift))
        {
            Find.WindowStack.Add(new LandformGraphEditor());
        }

        _lastEditedLandform = null;
        _lastEditorState = null;

        IsPreviewWindowOpen = false;
    }
}

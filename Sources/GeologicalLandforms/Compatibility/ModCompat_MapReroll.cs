using System;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Patching;
using NodeEditorFramework;
using UnityEngine;
using Verse;

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
        int seedInt = Gen.HashCombineInt(GenText.StableStringHash(seed), mapTile);
        Landform.PrepareMapGen(new IntVec2(mapSize, mapSize), mapTile, seedInt);
        Patch_RimWorld_GenStep_Terrain.Init();
    }

    [HarmonyPostfix]
    [HarmonyPriority(-10)]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Postfix()
    {
        Patch_RimWorld_GenStep_Terrain.CleanUp();
        Landform.CleanUp();
    }

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

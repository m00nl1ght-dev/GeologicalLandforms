using System;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(CellInspectorDrawer))]
internal static class Patch_Verse_CellInspectorDrawer
{
    [HarmonyPostfix]
    [HarmonyPatch("DrawWorldInspector")]
    private static void DrawWorldInspector()
    {
        var tile = GenWorld.MouseTile();
        if (tile < 0) return;

        var info = WorldTileInfo.Get(tile);
        var landform = info.Landforms?.FirstOrDefault(lf => !lf.IsLayer);

        if (landform != null)
        {
            var label = landform.TranslatedNameWithDirection(info.TopologyDirection).CapitalizeFirst();
            CellInspectorDrawer.DrawRow("GeologicalLandforms.WorldMap.Landform".Translate(), label);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("DrawMapInspector")]
    private static void DrawMapInspector()
    {
        var cell = UI.MouseCell();

        var biomeGrid = Find.CurrentMap?.BiomeGrid();
        if (biomeGrid is { Enabled: true })
        {
            CellInspectorDrawer.DrawRow("Biome_Label".Translate(), biomeGrid.BiomeAt(cell).LabelCap);
        }

        var waterInfo = Find.CurrentMap?.waterInfo;
        if (Prefs.DevMode && waterInfo?.riverFlowMap != null && Input.GetKey(KeyCode.LeftShift))
        {
            var x = waterInfo.riverFlowMap[waterInfo.riverFlowMapBounds.IndexOf(cell) * 2];
            var y = waterInfo.riverFlowMap[waterInfo.riverFlowMapBounds.IndexOf(cell) * 2 + 1];
            CellInspectorDrawer.DrawRow("River flow map", new Vector2(x, y).ToString());

            var buffer = new float[2];
            Buffer.BlockCopy(waterInfo.riverOffsetMap, waterInfo.riverFlowMapBounds.IndexOf(cell) * 8, buffer, 0, 8);
            CellInspectorDrawer.DrawRow("River offset map", new Vector2(buffer[0], buffer[1]).ToString());
        }
    }
}

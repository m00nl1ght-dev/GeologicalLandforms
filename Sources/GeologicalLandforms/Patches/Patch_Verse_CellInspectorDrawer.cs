using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

#if DEBUG
#if !RW_1_6_OR_GREATER
using System;
#endif
using System.Globalization;
using TerrainGraph;
using UnityEngine;
#endif

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
        if (info.Landforms != null)
        {
            var str = info.Landforms
                .Where(lf => !lf.IsInternal)
                .OrderBy(lf => lf.Manifest.TimeCreated)
                .Select(lf => lf.TranslatedNameWithDirection(info.TopologyDirection).CapitalizeFirst())
                .Join();

            if (str.Length > 0)
            {
                CellInspectorDrawer.DrawRow("GeologicalLandforms.WorldMap.Landform".Translate(), str);
            }
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

        #if DEBUG

        var waterInfo = Find.CurrentMap?.waterInfo;
        if (Prefs.DevMode && waterInfo?.riverFlowMap != null)
        {
            #if RW_1_6_OR_GREATER

            var i = cell.x * waterInfo.map.Size.x + cell.z;
            var x = waterInfo.riverFlowMap[i * 2];
            var y = waterInfo.riverFlowMap[i * 2 + 1];
            CellInspectorDrawer.DrawRow("River flow map", new Vector2(x, y).ToString());

            var px = waterInfo.flowMapPixels[cell.z * waterInfo.map.Size.x + cell.x];
            CellInspectorDrawer.DrawRow("River flow pixels", new Vector2(px.r, px.g).ToString());

            #else

            var x = waterInfo.riverFlowMap[waterInfo.riverFlowMapBounds.IndexOf(cell) * 2];
            var y = waterInfo.riverFlowMap[waterInfo.riverFlowMapBounds.IndexOf(cell) * 2 + 1];
            CellInspectorDrawer.DrawRow("River flow map", new Vector2(x, y).ToString());

            var buffer = new float[2];
            Buffer.BlockCopy(waterInfo.riverOffsetMap, waterInfo.riverFlowMapBounds.IndexOf(cell) * 8, buffer, 0, 8);
            CellInspectorDrawer.DrawRow("River offset map", new Vector2(buffer[0], buffer[1]).ToString());

            #endif

            var offset = new IntVec3(NodePathTrace.GridMarginDefault, 0, NodePathTrace.GridMarginDefault);
            CellInspectorDrawer.DrawRow("Flow grid pos", (cell + offset).ToString());

            if (NodePathTrace.TaskGrid != null)
            {
                var task = NodePathTrace.TaskGrid.ValueAt(cell.x, cell.z);
                CellInspectorDrawer.DrawRow("Path segment", task?.segment.Id.ToString(CultureInfo.InvariantCulture) ?? "None");
            }

            if (NodePathTrace.DebugGrid != null)
            {
                var debugValue = NodePathTrace.DebugGrid.ValueAt(cell.x, cell.z);
                CellInspectorDrawer.DrawRow("Path debug", debugValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        #endif
    }
}

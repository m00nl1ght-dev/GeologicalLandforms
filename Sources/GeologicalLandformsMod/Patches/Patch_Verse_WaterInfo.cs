#if DEBUG

using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using TerrainGraph.Flow;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WaterInfo))]
internal static class Patch_Verse_WaterInfo
{
    [HarmonyPostfix]
    [HarmonyPatch("DebugDrawRiver")]
    private static void DebugDrawRiver()
    {
        if (PathTracer.DebugLines != null && !LandformGraphEditor.IsEditorOpen)
        {
            var group = Input.GetKey(KeyCode.Y) ? 1 : Input.GetKey(KeyCode.X) ? 2 : 0;

            foreach (var line in PathTracer.DebugLines)
            {
                if (line.Group != 0 && line.Group != group) continue;

                var p1 = new Vector3((float) line.MapPos1.x, 0, (float) line.MapPos1.z);
                var p2 = new Vector3((float) line.MapPos2.x, 0, (float) line.MapPos2.z);

                if (p1 != p2)
                {
                    GenDraw.DrawLineBetween(p1, p2, (SimpleColor) (line.Color % 8));
                }
                else
                {
                    GenDraw.DrawCircleOutline(p1, 0.2f, (SimpleColor) (line.Color % 8));
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapInterface), "MapInterfaceOnGUI_BeforeMainTabs")]
    private static void MapInterfaceOnGUI_BeforeMainTabs()
    {
        if (DebugViewSettings.drawRiverDebug && PathTracer.DebugLines != null && !LandformGraphEditor.IsEditorOpen)
        {
            var group = Input.GetKey(KeyCode.Y) ? 1 : Input.GetKey(KeyCode.X) ? 2 : 0;

            foreach (var line in PathTracer.DebugLines)
            {
                if (line.Group != 0 && line.Group != group) continue;

                var p1 = new Vector3((float) line.MapPos1.x, 0, (float) line.MapPos1.z);
                var p2 = new Vector3((float) line.MapPos2.x, 0, (float) line.MapPos2.z);

                if (line.Label is { Length: > 0 })
                {
                    var p3 = Vector3.Lerp(p1, p2, 0.5f);

                    Vector2 vec = Find.Camera.WorldToScreenPoint(p3) / Prefs.UIScale;
                    vec.y = UI.screenHeight - vec.y - 1;

                    GenMapUI.DrawThingLabel(vec, line.Label, Color.white);
                }
            }
        }
    }
}

#endif

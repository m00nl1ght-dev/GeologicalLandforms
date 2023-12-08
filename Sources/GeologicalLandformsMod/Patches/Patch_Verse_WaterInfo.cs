#if DEBUG

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
        if (PathTracer.DebugLines != null)
        {
            var group = Input.GetKey(KeyCode.Y) ? 1 : Input.GetKey(KeyCode.X) ? 2 : 0;

            foreach (var line in PathTracer.DebugLines)
            {
                if (line.Group != 0 && line.Group != group) continue;

                var p1 = new Vector3((float) line.Pos1.x, 0, (float) line.Pos1.z);
                var p2 = new Vector3((float) line.Pos2.x, 0, (float) line.Pos2.z);

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
        if (DebugViewSettings.drawRiverDebug && PathTracer.DebugLines != null)
        {
            var group = Input.GetKey(KeyCode.Y) ? 1 : Input.GetKey(KeyCode.X) ? 2 : 0;

            foreach (var line in PathTracer.DebugLines)
            {
                if (line.Group != 0 && line.Group != group) continue;

                var p1 = new Vector3((float) line.Pos1.x, 0, (float) line.Pos1.z);
                var p2 = new Vector3((float) line.Pos2.x, 0, (float) line.Pos2.z);

                if (line.Label is { Length: > 0 })
                {
                    var p3 = Vector3.Lerp(p1, p2, 0.5f);
                    GenMapUI.DrawThingLabel((Vector3) GenMapUI.LabelDrawPosFor(p3.ToIntVec3()), line.Label, Color.white);
                }
            }
        }
    }
}

#endif

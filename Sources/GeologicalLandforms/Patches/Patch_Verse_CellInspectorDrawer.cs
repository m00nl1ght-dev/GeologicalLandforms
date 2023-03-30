using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
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
}

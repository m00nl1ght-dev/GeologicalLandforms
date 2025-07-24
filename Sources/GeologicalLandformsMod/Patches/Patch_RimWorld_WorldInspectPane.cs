#if RW_1_6_OR_GREATER

using GeologicalLandforms.TileEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldInspectPane))]
internal static class Patch_RimWorld_WorldInspectPane
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(WorldInspectPane.DoInspectPaneButtons))]
    private static void DoInspectPaneButtons_Postfix(Rect rect, ref float lineEndWidth)
    {
        var tile = Find.WorldSelector.SelectedTile;

        if (TileEditorWindow.CanEditTile(tile))
        {
            var btnRect = new Rect(rect.width - 72f, 0f, 24f, 24f);

            lineEndWidth += 24f;

            MouseoverSounds.DoRegion(btnRect);
            TooltipHandler.TipRegionByKey(btnRect, "GeologicalLandforms.WorldMap.EditTile");

            var color = Color.white;

            #if DEBUG
            if (Find.World.LandformData().HasData(tile.tileId)) color = new Color(0.5f, 0.5f, 1f);
            #endif

            if (Widgets.ButtonImage(btnRect, TileEditorWindow.IconEditTile, color))
            {
                GeologicalLandformsMod.LunarAPI.LifecycleHooks.DoOnce(() =>
                {
                    Find.WindowStack.Add(new TileEditorWindow(Find.World, tile));
                });
            }
        }
    }
}

#endif

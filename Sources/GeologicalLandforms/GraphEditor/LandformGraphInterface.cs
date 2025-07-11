using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Compatibility;
using LunarFramework.GUI;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class LandformGraphInterface
{
    public LandformGraphEditor Editor;
    public NodeEditorUserCache CanvasCache;
    public Landform Landform => (Landform) CanvasCache.nodeCanvas;

    public static float ToolbarHeight = 20;

    public bool ShowModalPanel;
    public Rect ModalPanelRect = new(20, 50, 250, 70);
    public Action ModalPanelContent;

    public void DrawToolbarGUI()
    {
        GUI.enabled = !ModCompat_MapReroll.IsPreviewWindowOpen;

        GUILayout.BeginHorizontal(GUI.skin.GetStyle("toolbar"));

        if (GUILayout.Button(new GUIContent(Landform?.Id != null
                        ? Landform.TranslatedNameForSelection.CapitalizeFirst()
                        : "GeologicalLandforms.Editor.Open".Translate(),
                    "GeologicalLandforms.Editor.Open.Tooltip".Translate()), GUI.skin.GetStyle("toolbarDropdown"),
                GUILayout.MinWidth(Landform?.Id != null ? 150f : 50f)))
        {
            var options = LandformManager.LandformsById.Values
                .OrderBy(e => e.TranslatedNameForSelection)
                .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () => Editor.OpenLandform(e)))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        GUILayout.Space(50f);

        if (Landform != null && Landform.Id != null)
        {
            if (GUILayout.Button("GeologicalLandforms.Editor.Tools".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(50f)))
                OpenToolsMenu();
        }

        GUILayout.FlexibleSpace();

        if (Landform != null && Landform.Id != null)
        {
            if (GUILayout.Button(Landform.GeneratingMapSize.x + "x" + Landform.GeneratingMapSize.z, GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(100f)))
            {
                var options = new[] { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 }
                    .Select(e => new FloatMenuOption(e + "x" + e, () =>
                    {
                        Landform.GeneratingMapSize = new IntVec2(e, e);
                        Editor.Refresh();
                    }))
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }

            GUILayout.Space(50f);

            var currentTileLabel = "None";

            if (Landform.GeneratingTile is WorldTileInfo worldTile)
            {
                currentTileLabel = $"World tile {worldTile.TileId} ({worldTile.Biome.LabelCap})";
            }
            else if (Landform.GeneratingTile is EditorMockTileInfo mockTile)
            {
                currentTileLabel = $"Simulated tile ({mockTile.Biome.LabelCap})";
            }

            if (GUILayout.Button(currentTileLabel, GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(250f)))
            {
                var options = new List<FloatMenuOption>();

                #if RW_1_6_OR_GREATER
                if (WorldRendererUtility.WorldRendered && WorldTileUtils.SelectedWorldTile is WorldTileInfo selTile)
                #else
                if (WorldRendererUtility.WorldRenderedNow && WorldTileUtils.SelectedWorldTile is WorldTileInfo selTile)
                #endif
                {
                    options.Add(new FloatMenuOption($"Selected world tile ({selTile.TileId})", () =>
                    {
                        Editor.ApplyRealTileContext(selTile);
                    }));
                }

                if (Find.Maps != null)
                {
                    foreach (var map in Find.Maps)
                    {
                        if (WorldTileInfo.Get(map) is WorldTileInfo mapTile)
                        {
                            options.Add(new FloatMenuOption($"World tile of map {map.Index + 1} ({mapTile.TileId})", () =>
                            {
                                Editor.ApplyRealTileContext(mapTile);
                            }));
                        }
                    }
                }

                options.Add(new FloatMenuOption("Simulated tile (custom)", () =>
                {
                    Editor.ApplyMockTileContext();
                    var window = LunarGUI.OpenGenericWindow(GeologicalLandformsAPI.LunarAPI, new Vector2(400, 400), DoMockTileUI);
                    window.absorbInputAroundWindow = false;
                    window.closeOnClickedOutside = false;
                    window.layer = WindowLayer.Super;
                    window.draggable = true;
                }));

                Find.WindowStack.Add(new FloatMenu(options));
            }

            GUILayout.Space(50f);

            if (GUILayout.Button("Reseed", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(60f)))
            {
                var newSeed = NodeBase.SeedSource.Next();
                foreach (var landform in Landform.GeneratingTile.Landforms) landform.RandSeed = newSeed;
                Editor.Refresh();
            }

            GUILayout.Space(200f);

            if (GUILayout.Button("Reset View", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(85f)))
            {
                Editor.NewEditorState();
                Landform.ResetView();
            }
        }

        GUI.backgroundColor = new Color(1, 0.3f, 0.3f, 1);

        if (GUILayout.Button("Close", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55)))
        {
            Editor.Close();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();
        GUI.enabled = true;

        if (Event.current.type == EventType.Repaint)
            ToolbarHeight = GUILayoutUtility.GetLastRect().yMax;
    }

    private void DoMockTileUI(Window window, LayoutRect layout)
    {
        if (Landform.GeneratingTile is EditorMockTileInfo mockTile)
        {
            mockTile.DoEditorGUI(layout, _ => Editor.Refresh());
        }
        else
        {
            window.Close();
        }
    }

    private void OpenToolsMenu()
    {
        var menu = new GenericMenu();

        if (Landform.IsCustom)
        {
            menu.AddItem(new GUIContent("GeologicalLandforms.Editor.Delete.Long".Translate()), true,
                () => ConfirmBox("GeologicalLandforms.Editor.ConfirmDelete".Translate(), Editor.Delete));
        }
        else
        {
            menu.AddItem(new GUIContent("GeologicalLandforms.Editor.Reset.Long".Translate()), true,
                () => ConfirmBox("GeologicalLandforms.Editor.ConfirmReset".Translate(), Editor.Reset));
        }

        menu.AddItem(new GUIContent("GeologicalLandforms.Editor.Copy.Long".Translate()), true, () => Editor.Duplicate());

        var forModDevs = "GeologicalLandforms.Editor.Tools.ForModDevs".Translate();

        if (Landform.ModContentPack != null && Landform.OriginalFileLocation != null)
        {
            var mcp = Landform.ModContentPack;
            var dest = mcp.ModMetaData.Source == ContentSource.ModsFolder;

            #if !DEBUG
            dest = dest && !mcp.PackageId.StartsWith("m00nl1ght");
            #endif

            if (dest)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent(forModDevs + "/" + "GeologicalLandforms.Editor.SaveInMod".Translate(mcp.Name)), true, () =>
                {
                    if (Landform.Manifest.IsEdited)
                    {
                        ConfirmBox("GeologicalLandforms.Editor.ConfirmSaveInMod".Translate(mcp.Name), Editor.SaveInMod);
                    }
                    else
                    {
                        var msg = "GeologicalLandforms.Editor.SaveInMod.NoChanges".Translate();
                        Messages.Message(msg, MessageTypeDefOf.SilentInput, false);
                    }
                });
            }
        }

        menu.ShowAsContext();
    }

    private void ConfirmBox(string text, Action action)
    {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, action));
    }

    public void DrawModalPanel()
    {
        if (ShowModalPanel)
        {
            if (ModalPanelContent == null)
                return;
            GUILayout.BeginArea(ModalPanelRect, GUI.skin.box);
            ModalPanelContent.Invoke();
            GUILayout.EndArea();
        }
    }

    private static void Tooltip(string tooltip)
    {
        if (Event.current.type == EventType.Repaint)
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            TooltipHandler.TipRegion(lastRect, new TipSignal(tooltip));
        }
    }
}

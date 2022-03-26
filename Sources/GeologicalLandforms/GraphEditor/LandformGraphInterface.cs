using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.ModCompat;
using NodeEditorFramework;
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
        GUI.enabled = Landform.GeneratingTile is null or EditorMockTileInfo && !ModCompat_MapReroll.IsPreviewWindowOpen;
        
        GUILayout.BeginHorizontal(GUI.skin.GetStyle("toolbar"));
        
        if (GUILayout.Button(new GUIContent(Landform?.TranslatedNameForSelection ?? "GeologicalLandforms.Editor.Open".Translate(),
                    "GeologicalLandforms.Editor.Open.Tooltip".Translate()), GUI.skin.GetStyle("toolbarDropdown"),
                GUILayout.MinWidth(Landform?.Id != null ? 150f : 50f)))
        {
            List<FloatMenuOption> options = LandformManager.Landforms.Values.Select(e =>
                new FloatMenuOption(e.Id, () => Editor.OpenLandform(e))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        GUILayout.Space(50f);

        if (Landform != null && Landform.Id != null)
        {
            if (GUILayout.Button("GeologicalLandforms.Editor.Copy".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(70f)))
                Editor.Duplicate();
            GuiUtils.Tooltip("GeologicalLandforms.Editor.Copy.Tooltip".Translate());

            if (!Landform.IsCustom)
            {
                if (GUILayout.Button("GeologicalLandforms.Editor.Reset".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55f))) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Editor.ConfirmReset".Translate(), Editor.Reset));
                GuiUtils.Tooltip("GeologicalLandforms.Editor.Reset.Tooltip".Translate());
            }

            if (Landform.IsCustom)
            {
                if (GUILayout.Button("GeologicalLandforms.Editor.Delete".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55f))) Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Editor.ConfirmDelete".Translate(), Editor.Delete));
                GuiUtils.Tooltip("GeologicalLandforms.Editor.Delete.Tooltip".Translate());
            }
        }

        GUILayout.FlexibleSpace();

        if (Landform != null && Landform.Id != null)
        {
            if (Prefs.DevMode && GUILayout.Button("Refresh", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(65f)))
            {
                Landform.TraverseAll();
            }

            if (GUILayout.Button("Reseed", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(60f)))
            {
                Landform.RandSeed = NodeBase.SeedSource.Next();
                Landform.TraverseAll();
            }
            
            GUILayout.Space(200f);
            
            if (GUILayout.Button("Reset View", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(85f)))
            {
                CanvasCache.NewEditorState();
                Landform?.ResetView();
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
}
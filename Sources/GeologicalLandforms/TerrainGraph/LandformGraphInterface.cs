﻿using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms;
using GeologicalLandforms.TerrainGraph;
using UnityEngine;
using Verse;

namespace NodeEditorFramework.Standard
{
	public class LandformGraphInterface
	{
		public LandformGraphEditor Editor;
		public NodeEditorUserCache CanvasCache;
		public Landform Landform => (Landform) CanvasCache.nodeCanvas;

		public static float toolbarHeight = 20;

		public bool showModalPanel;
		public Rect modalPanelRect = new(20, 50, 250, 70);
		public Action modalPanelContent;

		public void DrawToolbarGUI()
		{
			GUILayout.BeginHorizontal(GUI.skin.GetStyle("toolbar"));

			if (GUILayout.Button(new GUIContent(Landform?.Id ?? "GeologicalLandforms.Editor.Open".Translate(), 
				    "GeologicalLandforms.Editor.Open.Tooltip".Translate()), GUI.skin.GetStyle("toolbarDropdown"), 
				    GUILayout.MinWidth(Landform?.Id != null ? 150f : 50f)))
			{
				List<FloatMenuOption> options = LandformManager.CustomLandforms.Values.Select(e => 
					new FloatMenuOption(e.Id, () => Editor.Open(e))).ToList();
				Find.WindowStack.Add(new FloatMenu(options));
			}

			if (Landform != null && Landform.Id != null)
			{
				GUILayout.Space(50f);
			
				if (GUILayout.Button("GeologicalLandforms.Editor.Copy".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(57f)))
					Editor.Duplicate();
				GuiUtils.Tooltip("GeologicalLandforms.Editor.Copy.Tooltip".Translate());

				if (!Landform.IsCustom && GUILayout.Button("GeologicalLandforms.Editor.Reset".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55f)))
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Editor.ConfirmReset".Translate(), Editor.Reset));
				GuiUtils.Tooltip("GeologicalLandforms.Editor.Reset.Tooltip".Translate());
				
				if (Landform.IsCustom && GUILayout.Button("GeologicalLandforms.Editor.Delete".Translate(), GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55f)))
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Editor.ConfirmDelete".Translate(), Editor.Delete));
				GuiUtils.Tooltip("GeologicalLandforms.Editor.Delete.Tooltip".Translate());
			}

			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("Reset View", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(85f)))
			{
				CanvasCache.NewEditorState();
				Landform?.ResetView();
			}

			GUI.backgroundColor = new Color(1, 0.3f, 0.3f, 1);
			if (GUILayout.Button("Close", GUI.skin.GetStyle("toolbarButton"), GUILayout.MinWidth(55)))
				Editor.Close();
			GUI.backgroundColor = Color.white;

			GUILayout.EndHorizontal();
			if (Event.current.type == EventType.Repaint)
				toolbarHeight = GUILayoutUtility.GetLastRect().yMax;
		}

		public void DrawModalPanel()
		{
			if (showModalPanel)
			{
				if (modalPanelContent == null)
					return;
				GUILayout.BeginArea(modalPanelRect, GUI.skin.box);
				modalPanelContent.Invoke();
				GUILayout.EndArea();
			}
		}
	}
}

using System;
using LunarFramework.GUI;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Layer Config", 9000)]
public class NodeUILayerConfig : NodeUIBase
{
    public const string ID = "layerConfig";
    public override string GetID => ID;

    public override string Title => "Layer Config";
    public override Vector2 DefaultSize => new(400, 75);

    public int Priority;

    private string _priorityEdit;

    protected override void DoWindowContents(LayoutRect layout)
    {
        LunarGUI.PushEnabled(Landform.IsCustom);
        
        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.3f), "GeologicalLandforms.Settings.Landform.Priority".Translate());
        LunarGUI.IntField(layout, ref Priority, ref _priorityEdit, -999, 999);
        layout.End();
        
        LunarGUI.PopEnabled();
    }

    public override void DrawNode()
    {
        if (Landform.Id != null) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.LayerConfig;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.LayerConfig = this;
    }
    
    protected override void OnDelete()
    {
        if (Landform.LayerConfig == this) Landform.LayerConfig = null;
    }
}
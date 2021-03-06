using System;
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
    public override Vector2 DefaultSize => new(400, 80);

    public int Priority;

    private string _priorityEdit;

    protected override void DoWindowContents(Listing_Standard listing)
    {
        GUI.enabled = Landform.IsCustom;

        GuiUtils.IntEntry(listing, "Priority", ref Priority, ref _priorityEdit, -999, 999);

        GUI.enabled = true;
        if (GUI.changed) TerrainCanvas.OnNodeChange(this);
    }

    public override void DrawNode()
    {
        if (Landform.Id != null) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeUILayerConfig existing = Landform.LayerConfig;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.LayerConfig = this;
    }
    
    protected override void OnDelete()
    {
        if (Landform.LayerConfig == this) Landform.LayerConfig = null;
    }
}
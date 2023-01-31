using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Scatterers", 405)]
public class NodeOutputScatterers : NodeOutputBase
{
    public const string ID = "outputScatterers";
    public override string GetID => ID;

    public override string Title => "Scatterers";

    [ValueConnectionKnob("Mineables", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob MineablesKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(MineablesKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        MineablesKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputScatterers existing = Landform.OutputScatterers;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputScatterers = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputScatterers == this) Landform.OutputScatterers = null;
    }

    public double? GetMineables()
    {
        return MineablesKnob.GetValue<ISupplier<double>>()?.ResetAndGet();
    }
}

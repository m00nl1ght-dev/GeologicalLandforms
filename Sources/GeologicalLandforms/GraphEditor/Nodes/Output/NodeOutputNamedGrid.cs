using System;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Named Grid", 420)]
public class NodeOutputNamedGrid : NodeOutputBase
{
    public const string ID = "outputNamedGrid";
    public override string GetID => ID;

    public override string Title => "Named Grid Output";

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    [ValueConnectionKnob("Grid", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public string Name = "unnamed";

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        Name = RTEditorGUI.TextField(Name, DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InputKnobRef.SetPosition();
        OutputKnobRef?.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI) => Landform.NamedGridOutputs.Add(this);

    protected override void OnDelete() => Landform.NamedGridOutputs.Remove(this);

    public IGridFunction<double> Get()
    {
        return InputKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }
}

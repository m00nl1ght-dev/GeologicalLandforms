using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Roof/Grid/Const", 335)]
public class NodeRoofGridFromValue : NodeBase
{
    public const string ID = "roofGridFromValue";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";

    [ValueConnectionKnob("Input", Direction.In, RoofFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        RoofData.RoofSelector(this, Value, !InputKnob.connected(), selected =>
        {
            Value = RoofData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        var input = GetIfConnected<RoofData>(InputKnob);
        if (input != null) Value = RoofData.ToString(input.ResetAndGet().Roof);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<RoofData>>>(new NodeGridFromValue.Output<RoofData>(
            SupplierOrFixed(InputKnob, RoofData.FromString(Value))
        ));
        return true;
    }
}

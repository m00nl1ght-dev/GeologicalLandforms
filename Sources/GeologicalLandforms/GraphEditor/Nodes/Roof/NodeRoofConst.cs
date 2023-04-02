using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Roof/Const", 330)]
public class NodeRoofConst : NodeBase
{
    public const string ID = "roofConst";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Const";

    [ValueConnectionKnob("Output", Direction.Out, RoofFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        RoofData.RoofSelector(this, Value, true, selected =>
        {
            Value = RoofData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<RoofData>>(Supplier.Of(RoofData.FromString(Value)));
        return true;
    }
}

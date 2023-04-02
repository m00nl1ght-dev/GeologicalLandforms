using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Terrain/Const", 310)]
public class NodeTerrainConst : NodeBase
{
    public const string ID = "terrainConst";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Const";

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        TerrainData.TerrainSelector(this, Value, true, selected =>
        {
            Value = TerrainData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(TerrainData.FromString(Value)));
        return true;
    }
}

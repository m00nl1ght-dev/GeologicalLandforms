using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Biome/Const", 340)]
public class NodeBiomeConst : NodeBase
{
    public const string ID = "biomeConst";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Const";

    [ValueConnectionKnob("Output", Direction.Out, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        BiomeData.BiomeSelector(this, Value, true, selected =>
        {
            Value = BiomeData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<BiomeData>>(Supplier.Of(BiomeData.FromString(Value)));
        return true;
    }
}

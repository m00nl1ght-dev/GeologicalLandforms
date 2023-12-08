using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Biome/Grid/Const", 345)]
public class NodeBiomeGridFromValue : NodeBase
{
    public const string ID = "biomeGridFromValue";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";

    [ValueConnectionKnob("Input", Direction.In, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        BiomeData.BiomeSelector(this, Value, !InputKnob.connected(), selected =>
        {
            Value = BiomeData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        var input = GetIfConnected<BiomeData>(InputKnob);
        if (input != null) Value = BiomeData.ToString(input.ResetAndGet().Biome);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<BiomeData>>>(new NodeGridFromValue.Output<BiomeData>(
            SupplierOrFallback(InputKnob, BiomeData.FromString(Value))
        ));
        return true;
    }
}

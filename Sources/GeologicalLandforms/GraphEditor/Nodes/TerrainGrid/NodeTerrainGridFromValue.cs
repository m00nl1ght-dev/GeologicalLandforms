using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Grid/Const", 300)]
public class NodeTerrainGridFromValue : NodeBase
{
    public const string ID = "terrainGridFromValue";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";

    [ValueConnectionKnob("Input", Direction.In, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Value;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        TerrainData.TerrainSelector(this, Value, !InputKnob.connected(), selected =>
        {
            Value = TerrainData.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        var input = GetIfConnected<TerrainData>(InputKnob);
        if (input != null) Value = TerrainData.ToString(input.ResetAndGet().Terrain);
    }
    
    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainData>>>(new NodeGridFromValue.Output<TerrainData>(
            SupplierOrFallback(InputKnob, TerrainData.FromString(Value))
        ));
        return true;
    }
}

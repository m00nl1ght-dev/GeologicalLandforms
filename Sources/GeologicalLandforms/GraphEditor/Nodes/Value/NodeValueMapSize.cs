using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Map Size", 1001)]
public class NodeValueMapSize : NodeBase
{
    public const string ID = "mapSize";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Map Size";

    [ValueConnectionKnob("Output", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<double>>(Supplier.Of<double>(Landform.GeneratingMapSizeMin));
        return true;
    }
}

using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Roof/Const", 330)]
public class NodeRoofGridFromValue : NodeBase
{
    public const string ID = "roofGridFromValue";
    public override string GetID => ID;

    public override Vector2 DefaultSize => new(60, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";

    [ValueConnectionKnob("Input", Direction.In, RoofFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<RoofData>>>(new Output(
            SupplierOrFixed(InputKnob, RoofData.Empty)
        ));
        return true;
    }

    public class Output : ISupplier<IGridFunction<RoofData>>
    {
        private readonly ISupplier<RoofData> _input;

        public Output(ISupplier<RoofData> input)
        {
            _input = input;
        }

        public IGridFunction<RoofData> Get()
        {
            return GridFunction.Of(_input.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}

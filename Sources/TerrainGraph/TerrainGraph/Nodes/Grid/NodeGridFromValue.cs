using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Const", 210)]
public class NodeGridFromValue : NodeBase
{
    public const string ID = "gridFromValue";
    public override string GetID => ID;
    
    public override Vector2 DefaultSize => new(60, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";
    
    [ValueConnectionKnob("Input", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrValueFixed(InputKnob, 0)
        ));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<double> _input;

        public Output(ISupplier<double> input)
        {
            _input = input;
        }

        public IGridFunction<double> Get()
        {
            return GridFunction.Of(_input.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}
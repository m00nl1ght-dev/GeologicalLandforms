using System;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace TerrainGraph;

[Serializable]
[Node(false, "Terrain Grid/Const")]
public class NodeTerrainGridFromValue : NodeBase
{
    public const string ID = "terrainGridFromValue";
    public override string GetID => ID;
    
    public override Vector2 DefaultSize => new(60, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";
    
    [ValueConnectionKnob("Input", Direction.In, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainDef>>>(new Output(
            SupplierOrFixed<TerrainDef>(InputKnob, null)
        ));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<TerrainDef>>
    {
        private readonly ISupplier<TerrainDef> _input;

        public Output(ISupplier<TerrainDef> input)
        {
            _input = input;
        }

        public IGridFunction<TerrainDef> Get()
        {
            return GridFunction.Of(_input.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}
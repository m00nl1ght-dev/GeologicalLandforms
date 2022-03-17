using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Const", 300)]
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
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainData>>>(new Output(
            SupplierOrFixed(InputKnob, TerrainData.Empty)
        ));
        return true;
    }
    
    public class Output : ISupplier<IGridFunction<TerrainData>>
    {
        private readonly ISupplier<TerrainData> _input;

        public Output(ISupplier<TerrainData> input)
        {
            _input = input;
        }

        public IGridFunction<TerrainData> Get()
        {
            return GridFunction.Of(_input.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}
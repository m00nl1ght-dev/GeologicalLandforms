using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Map Transform", 212)]
public class NodeGridTransformByMapSize : NodeBase
{
    public Landform Landform => (Landform) canvas;

    public const string ID = "gridTransformByMapSize";
    public override string GetID => ID;

    public override string Title => "Map Transform";

    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Input", BoxLayout);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrFallback(InputKnob, GridFunction.Zero),
            Landform.MapSpaceToNodeSpaceFactor
        ));
        return true;
    }

    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<IGridFunction<double>> _input;
        private readonly double _mapScale;

        public Output(ISupplier<IGridFunction<double>> input, double mapScale)
        {
            _input = input;
            _mapScale = mapScale;
        }

        public IGridFunction<double> Get()
        {
            return new GridFunction.Transform<double>(_input.Get(), _mapScale);
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}

using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Rotate", 212)]
public class NodeGridRotate : NodeBase
{
    public const string ID = "gridRotate";
    public override string GetID => ID;

    public override string Title => "Rotate";
    
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("Angle", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob AngleKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public double Angle;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Input", BoxLayout);
        GUILayout.EndHorizontal();
        
        KnobValueField(AngleKnob, ref Angle);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    public override void RefreshPreview()
    {
        ISupplier<double> supplier = GetIfConnected<double>(AngleKnob);
        if (supplier != null) Angle = supplier.ResetAndGet();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrGridFixed(InputKnob, GridFunction.Zero), 
            SupplierOrValueFixed(AngleKnob, Angle),
            GridSize / 2, GridSize / 2
        ));
        return true;
    }
    
    public class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<IGridFunction<double>> _input;
        private readonly ISupplier<double> _angle;
        private readonly double _pivotX;
        private readonly double _pivotY;

        public Output(ISupplier<IGridFunction<double>> input, ISupplier<double> angle, double pivotX, double pivotY)
        {
            _input = input;
            _angle = angle;
            _pivotX = pivotX;
            _pivotY = pivotY;
        }

        public IGridFunction<double> Get()
        {
            return new GridFunction.Rotate<double>(_input.Get(), _pivotX, _pivotY, _angle.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
            _angle.ResetState();
        }
    }
}
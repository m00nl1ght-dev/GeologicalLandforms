using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Rotate")]
public class NodeGridRotate : NodeBase
{
    public const string ID = "gridRotate";
    public override string GetID => ID;

    public override string Title => "Rotate";
    public override Vector2 DefaultSize => new(200, 85);
    
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
        GUILayout.Label("Input");
        GUILayout.EndHorizontal();
        
        KnobValueField(AngleKnob, ref Angle);

        GUILayout.EndVertical();

        if (GUI.changed)
            NodeEditor.curNodeCanvas.OnNodeChange(this);
    }
    
    public override void RefreshPreview()
    {
        RefreshIfConnected(AngleKnob, ref Angle);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<GridFunction>>(new Output(
            SupplierOrFixed(InputKnob, GridFunction.Zero), 
            SupplierOrFixed(AngleKnob, Angle),
            GridSize / 2, GridSize / 2
        ));
        return true;
    }
    
    private class Output : ISupplier<GridFunction>
    {
        private readonly ISupplier<GridFunction> _input;
        private readonly ISupplier<double> _angle;
        private readonly double _pivotX;
        private readonly double _pivotY;

        public Output(ISupplier<GridFunction> input, ISupplier<double> angle, double pivotX, double pivotY)
        {
            _input = input;
            _angle = angle;
            _pivotX = pivotX;
            _pivotY = pivotY;
        }

        public GridFunction Get()
        {
            return new GridFunction.Rotate(_input.Get(), _pivotX, _pivotY, _angle.Get());
        }

        public void ResetState()
        {
            _input.ResetState();
            _angle.ResetState();
        }
    }
}
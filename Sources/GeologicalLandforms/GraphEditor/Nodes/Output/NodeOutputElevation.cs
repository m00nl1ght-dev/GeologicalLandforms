using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Elevation", 400)]
public class NodeOutputElevation : NodeOutputBase
{
    public const string ID = "outputElevation";
    public override string GetID => ID;

    public override string Title => "Elevation Output";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    [ValueConnectionKnob("Elevation", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("ElevationOutput", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputElevation existing = Landform.OutputElevation;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputElevation = this;
    }

    public IGridFunction<double> Get()
    {
        IGridFunction<double> function = InputKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
        return function == null ? GridFunction.Zero : ScaleWithMap(function);
    }
    
    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }
}
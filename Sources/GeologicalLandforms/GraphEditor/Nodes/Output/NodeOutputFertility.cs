using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Fertility", 401)]
public class NodeOutputFertility : NodeOutputBase
{
    public const string ID = "outputFertility";
    public override string GetID => ID;

    public override string Title => "Fertility Output";
    
    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    [ValueConnectionKnob("Fertility", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("FertilityOutput", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputFertility exiting = Landform.OutputFertility;
        if (exiting != null && exiting != this && canvas.nodes.Contains(exiting)) exiting.Delete();
        Landform.OutputFertility = this;
    }
    
    protected override void OnDelete()
    {
        if (Landform.OutputFertility == this) Landform.OutputFertility = null;
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
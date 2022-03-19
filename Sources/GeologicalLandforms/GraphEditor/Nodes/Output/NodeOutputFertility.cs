using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Fertility", 400)]
public class NodeOutputFertility : NodeOutputBase
{
    public const string ID = "outputFertility";
    public override string GetID => ID;

    public override string Title => "Fertility Output";
    
    public override ValueConnectionKnob InputKnobRef => InputKnob;
    
    [ValueConnectionKnob("Fertility", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputFertility exiting = Landform.OutputFertility;
        if (exiting != null && exiting != this && canvas.nodes.Contains(exiting)) exiting.Delete();
        Landform.OutputFertility = this;
    }
    
    public IGridFunction<double> Get()
    {
        IGridFunction<double> function = InputKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
        return function == null ? GridFunction.Zero : ScaleWithMap(function);
    }
}
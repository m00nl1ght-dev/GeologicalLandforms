using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Caves", 405)]
public class NodeOutputCaves : NodeOutputBase
{
    public const string ID = "outputCaves";
    public override string GetID => ID;

    public override string Title => "Caves Output";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    [ValueConnectionKnob("Caves", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("CavesOutput", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void OnCreate(bool fromGUI)
    {
        var exiting = Landform.OutputCaves;
        if (exiting != null && exiting != this && canvas.nodes.Contains(exiting)) exiting.Delete();
        Landform.OutputCaves = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputCaves == this) Landform.OutputCaves = null;
    }

    public IGridFunction<double> Get()
    {
        return InputKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }
}

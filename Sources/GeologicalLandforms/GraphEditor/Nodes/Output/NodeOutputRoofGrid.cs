using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Roof Grid", 407)]
public class NodeOutputRoofGrid : NodeOutputBase
{
    public const string ID = "outputRoofGrid";
    public override string GetID => ID;

    public override string Title => "Roof Output";

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    [ValueConnectionKnob("Roof Grid", Direction.In, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override void OnCreate(bool fromGUI)
    {
        var exiting = Landform.OutputRoofGrid;
        if (exiting != null && exiting != this && canvas.nodes.Contains(exiting)) exiting.Delete();
        Landform.OutputRoofGrid = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputRoofGrid == this) Landform.OutputRoofGrid = null;
    }

    public IGridFunction<RoofData> Get()
    {
        return InputKnob.GetValue<ISupplier<IGridFunction<RoofData>>>()?.ResetAndGet();
    }
}

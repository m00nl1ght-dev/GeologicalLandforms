using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Caves", 354)]
public class NodeInputCaves : NodeInputBase
{
    public const string ID = "inputCaves";
    public override string GetID => ID;

    public override string Title => "Caves Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Caves", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputCaves;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputCaves = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputCaves == this) Landform.InputCaves = null;
    }

    public override bool Calculate()
    {
        var supplier = Landform.GetFeature(l => l.OutputCaves?.InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        if (supplier != null)
        {
            Knob.SetValue(supplier);
            return true;
        }

        if (!Landform.GeneratingTile.HasCaves)
        {
            Knob.SetValue(Supplier.Of(GridFunction.Zero));
            return true;
        }

        Knob.SetValue(BuildVanillaCaveGridSupplier());
        return true;
    }
}

using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Biome Grid", 353)]
public class NodeInputBiomeGrid : NodeInputBase
{
    public const string ID = "inputBiomeGrid";
    public override string GetID => ID;

    public override string Title => "Biome Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Biome Grid", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputBiomeGrid;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputBiomeGrid = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputBiomeGrid == this) Landform.InputBiomeGrid = null;
    }

    public override bool Calculate()
    {
        var supplier = Landform.GetFeature(l => l.OutputBiomeGrid?.BiomeGridKnob.GetValue<ISupplier<IGridFunction<BiomeData>>>());
        supplier ??= Supplier.Of(GridFunction.Of(new BiomeData(Landform.GeneratingTile.Biome)));
        Knob.SetValue(supplier);
        return true;
    }
}

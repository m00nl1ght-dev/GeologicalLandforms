using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using Verse;

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
        #if RW_1_6_OR_GREATER

        if (MapGenerator.mapBeingGenerated != null)
        {
            var biomeGrid = MapGenerator.mapBeingGenerated.BiomeGrid();
            if (biomeGrid != null)
            {
                Knob.SetValue(Supplier.Of(Landform.TransformIntoNodeSpace(new DiscreteBiomeGridWrapper(biomeGrid))));
                return true;
            }
        }

        #endif

        var supplier = GetFromBelowStack(Landform, l => l.OutputBiomeGrid?.BiomeGridKnob.GetValue<ISupplier<IGridFunction<BiomeDef>>>());
        supplier ??= Supplier.Of(GridFunction.Of<BiomeDef>(null));
        Knob.SetValue(supplier);
        return true;
    }
}

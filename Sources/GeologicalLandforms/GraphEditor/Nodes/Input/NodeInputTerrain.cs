using System;
using NodeEditorFramework;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Terrain", 352)]
public class NodeInputTerrain : NodeInputBase
{
    public const string ID = "inputTerrain";
    public override string GetID => ID;

    public override string Title => "Terrain Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Base", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputTerrain;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputTerrain = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputTerrain == this) Landform.InputTerrain = null;
    }

    public override bool Calculate()
    {
        #if RW_1_6_OR_GREATER

        if (MapGenerator.mapBeingGenerated is { terrainGrid: not null } map)
        {
            Knob.SetValue(Supplier.From(() => Landform.TransformIntoNodeSpace(new DiscreteTerrainGridWrapper(map.terrainGrid, map.Size, null))));
            return true;
        }

        #endif

        var supplier = GetFromBelowStack(Landform, l => l.OutputTerrain?.BaseKnob.GetValue<ISupplier<IGridFunction<TerrainDef>>>());
        supplier ??= Supplier.Of(GridFunction.Of<TerrainDef>(null));
        Knob.SetValue(supplier);
        return true;
    }
}

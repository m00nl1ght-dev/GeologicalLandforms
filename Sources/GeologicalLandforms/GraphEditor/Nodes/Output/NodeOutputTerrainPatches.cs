using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Terrain Patches", 403)]
public class NodeOutputTerrainPatches : NodeOutputBase
{
    public const string ID = "outputTerrainPatches";
    public override string GetID => ID;

    public override string Title => "Terrain Patches";

    public override ValueConnectionKnob InputKnobRef => OffsetKnob;

    [ValueConnectionKnob("Offset", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob OffsetKnob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputTerrainPatches;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputTerrainPatches = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputTerrainPatches == this) Landform.OutputTerrainPatches = null;
    }

    public IGridFunction<double> GetOffset()
    {
        return OffsetKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }
}

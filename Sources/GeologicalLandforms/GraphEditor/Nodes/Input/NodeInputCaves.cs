using System;
using NodeEditorFramework;
using TerrainGraph;
using Verse;

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
        #if RW_1_6_OR_GREATER

        if (MapGenerator.mapBeingGenerated is {} map)
        {
            SetOutput(Knob, Supplier.From(() => Landform.TransformIntoNodeSpace(new DiscreteFloatGridWrapper(MapGenerator.Caves, map.Size, 0d))));
            return true;
        }

        #endif

        var supplier = GetFromBelowStack(Landform, l => l.OutputCaves?.InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        if (supplier != null)
        {
            SetOutput(Knob, supplier);
            return true;
        }

        if (!Landform.GeneratingTile.HasCaves)
        {
            SetOutput(Knob, Supplier.Of(GridFunction.Zero));
            return true;
        }

        SetOutput(Knob, BuildVanillaCaveGridSupplier());
        return true;
    }
}

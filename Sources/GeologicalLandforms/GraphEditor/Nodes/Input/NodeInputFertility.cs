using System;
using NodeEditorFramework;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Fertility", 351)]
public class NodeInputFertility : NodeInputBase
{
    public const string ID = "inputFertility";
    public override string GetID => ID;

    public override string Title => "Fertility Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Fertility", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputFertility;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputFertility = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputFertility == this) Landform.InputFertility = null;
    }

    public override bool Calculate()
    {
        #if RW_1_6_OR_GREATER

        if (MapGenerator.mapBeingGenerated != null)
        {
            Knob.SetValue(Supplier.Of(Landform.TransformIntoNodeSpace(new DiscreteFloatGridWrapper(MapGenerator.Fertility))));
            return true;
        }

        #endif

        var supplier = GetFromBelowStack(Landform, l => l.OutputFertility?.InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        supplier ??= BuildVanillaFertilityGridSupplier();
        Knob.SetValue(supplier);
        return true;
    }
}

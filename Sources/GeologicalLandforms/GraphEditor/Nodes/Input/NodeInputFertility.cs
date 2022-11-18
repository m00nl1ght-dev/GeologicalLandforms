using System;
using NodeEditorFramework;
using TerrainGraph;

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
        var func = Landform.GetFeature(l => l.OutputFertility?.Get());

        if (func == null)
        {
            var seed = TryGetVanillaGenStepRandValue(826504671, 1);
            func = NodeOutputFertility.BuildVanillaFertilityGrid(Landform.GeneratingTile, seed);
        }
        
        Knob.SetValue(func);
        return true;
    }
}
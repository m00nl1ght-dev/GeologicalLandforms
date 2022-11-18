using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Elevation", 350)]
public class NodeInputElevation : NodeInputBase
{
    public const string ID = "inputElevation";
    public override string GetID => ID;

    public override string Title => "Elevation Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Elevation", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputElevation;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputElevation = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputElevation == this) Landform.InputElevation = null;
    }

    public override bool Calculate()
    {
        var func = Landform.GetFeature(l => l.OutputElevation?.Get());

        if (func == null)
        {
            var seed = TryGetVanillaGenStepRandValue(826504671, 0);
            func = NodeOutputElevation.BuildVanillaElevationGrid(Landform.GeneratingTile, seed);
        }
        
        Knob.SetValue(func);
        return true;
    }
}
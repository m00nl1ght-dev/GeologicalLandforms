using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Roof/Grid/Preview", 336)]
public class NodeRoofGridPreview : NodeDiscreteGridPreview<RoofData>
{
    public const string ID = "roofGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<RoofData> Default => GridFunction.Of(RoofData.Empty);

    [ValueConnectionKnob("Input", Direction.In, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<RoofData>>>());
        return true;
    }

    protected override string MakeTooltip(RoofData value, double x, double y)
    {
        return RoofData.DislayString(value.Roof) + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
    }

    private static List<Color> _roofColors = new()
    {
        new Color(0.2f, 0.2f, 0.2f),
        new Color(0.4f, 0.4f, 0.4f),
        new Color(0.6f, 0.6f, 0.6f),
        new Color(0.8f, 0.8f, 0.8f),
        new Color(1f, 1f, 1f)
    };

    protected override Color GetColor(RoofData value)
    {
        if (value.IsEmpty) return Color.black;
        return GetRoofColor(value.Roof, value.SelectionIndex);
    }

    public Color GetRoofColor(RoofDef def, int selIdx)
    {
        return _roofColors[selIdx % _roofColors.Count];
    }
}

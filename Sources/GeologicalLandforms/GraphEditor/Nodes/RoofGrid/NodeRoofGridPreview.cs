using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Roof/Grid/Preview", 336)]
public class NodeRoofGridPreview : NodeDiscreteGridPreview<RoofDef>
{
    public const string ID = "roofGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<RoofDef> Default => GridFunction.Of<RoofDef>(null);

    [ValueConnectionKnob("Input", Direction.In, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    private readonly List<RoofDef> _colorIdxCache = [];

    public override bool Calculate()
    {
        _colorIdxCache.Clear();

        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<RoofDef>>>());
        return true;
    }

    protected override string MakeTooltip(RoofDef value, double x, double y)
    {
        var displayName = RoofFunctionConnection.Instance.DisplayName(value);
        return $"{displayName} ( {Math.Round(x, 0)} | {Math.Round(y, 0)} )";
    }

    protected override Color GetColor(RoofDef value)
    {
        if (value == null) return Color.black;

        var idx = _colorIdxCache.IndexOf(value);

        if (idx < 0)
        {
            idx = _colorIdxCache.Count;
            _colorIdxCache.Add(value);
        }

        return NodeGridPreview.FixedColors[idx % NodeGridPreview.FixedColors.Count];
    }
}

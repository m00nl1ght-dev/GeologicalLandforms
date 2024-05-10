using System;
using System.Collections.Generic;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Biome/Grid/Preview", 346)]
public class NodeBiomeGridPreview : NodeDiscreteGridPreview<BiomeDef>
{
    public const string ID = "biomeGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<BiomeDef> Default => GridFunction.Of<BiomeDef>(null);

    [ValueConnectionKnob("Input", Direction.In, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    private readonly List<BiomeDef> _colorIdxCache = [];

    public override bool Calculate()
    {
        _colorIdxCache.Clear();

        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<BiomeDef>>>());
        return true;
    }

    protected override string MakeTooltip(BiomeDef value, double x, double y)
    {
        var displayName = BiomeFunctionConnection.Instance.DisplayName(value);
        return $"{displayName} ( {Math.Round(x, 0)} | {Math.Round(y, 0)} )";
    }

    protected override Color GetColor(BiomeDef value)
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

using System;
using System.Collections.Generic;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Biome/Grid/Preview", 346)]
public class NodeBiomeGridPreview : NodeDiscreteGridPreview<BiomeData>
{
    public const string ID = "biomeGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<BiomeData> Default => GridFunction.Of(BiomeData.Empty);

    [ValueConnectionKnob("Input", Direction.In, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<BiomeData>>>());
        return true;
    }

    protected override string MakeTooltip(BiomeData value, double x, double y)
    {
        return BiomeData.DislayString(value.Biome) + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
    }

    private static List<Color> _biomeColors =
    [
        new Color(0.2f, 0.2f, 0.2f),
        new Color(0.4f, 0.4f, 0.4f),
        new Color(0.6f, 0.6f, 0.6f),
        new Color(0.8f, 0.8f, 0.8f),
        new Color(1f, 1f, 1f)
    ];

    protected override Color GetColor(BiomeData value)
    {
        if (value.IsEmpty) return Color.black;
        return GetBiomeColor(value.Biome, value.SelectionIndex);
    }

    public Color GetBiomeColor(BiomeDef def, int selIdx)
    {
        return _biomeColors[selIdx % _biomeColors.Count];
    }
}

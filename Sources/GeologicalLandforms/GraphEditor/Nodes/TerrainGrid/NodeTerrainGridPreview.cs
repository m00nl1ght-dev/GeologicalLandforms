using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Preview", 320)]
public class NodeTerrainGridPreview : NodeDiscreteGridPreview<TerrainData>
{
    public const string ID = "terrainGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<TerrainData> Default => GridFunction.Of(TerrainData.Empty);
    
    [ValueConnectionKnob("Input", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<TerrainData>>>());
        return true;
    }

    protected override string MakeTooltip(TerrainData value, double x, double y)
    {
        return value + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
    }

    private static List<Color> _terrainColors = new()
    {
        new Color(0.2f, 0.2f, 0.2f),
        new Color(0.4f, 0.4f, 0.4f),
        new Color(0.6f, 0.6f, 0.6f),
        new Color(0.8f, 0.8f, 0.8f),
        new Color(1f, 1f, 1f)
    };

    protected override Color GetColor(TerrainData value)
    {
        if (value.IsEmpty) return Color.black;
        return _terrainColors[value.SelectionIndex % _terrainColors.Count];
    }
}
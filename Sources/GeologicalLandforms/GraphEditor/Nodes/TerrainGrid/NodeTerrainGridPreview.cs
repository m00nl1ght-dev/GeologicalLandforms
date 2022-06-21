using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Preview", 320)]
public class NodeTerrainGridPreview : NodeDiscreteGridPreview<TerrainData>
{
    public static readonly Color RockColor = GenColor.FromHex("36271C");
    
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
    
    [ValueConnectionKnob("ElevationInput", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob ElevationInputKnob;
    
    [ValueConnectionKnob("ElevationOutput", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob ElevationOutputKnob;
    
    [NonSerialized] 
    protected IGridFunction<double> ElevationFunction;
    
    public override void CleanUpGUI()
    {
        base.CleanUpGUI();
        ElevationFunction = null;
    }
    
    public override void NodeGUI()
    {
        base.NodeGUI();
        ElevationInputKnob.SetPosition(DefaultSize.y - FirstKnobPosition);
        ElevationOutputKnob.SetPosition(DefaultSize.y - FirstKnobPosition);
    }
    
    public override void RefreshPreview()
    {
        var previewRatio = TerrainCanvas.GridPreviewRatio;
        PreviewFunction = InputKnob.connected() ? InputKnobRef.GetValue<ISupplier<IGridFunction<TerrainData>>>().ResetAndGet() : Default;
        ElevationFunction = ElevationInputKnob.connected() ? ElevationInputKnob.GetValue<ISupplier<IGridFunction<double>>>().ResetAndGet() : GridFunction.Zero;
        
        for (int x = 0; x < TerrainCanvas.GridPreviewSize; x++)
        {
            for (int y = 0; y < TerrainCanvas.GridPreviewSize; y++)
            {
                var value = PreviewFunction.ValueAt(x * previewRatio, y * previewRatio);
                var elevation = ElevationFunction.ValueAt(x * previewRatio, y * previewRatio);
                var color = elevation > 0.7f ? RockColor : GetColor(value);
                PreviewTexture.SetPixel(x, y, color);
            }
        }
        
        PreviewTexture.Apply();
    }
    
    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<TerrainData>>>());
        ElevationOutputKnob.SetValue(ElevationInputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }

    protected override string MakeTooltip(TerrainData value, double x, double y)
    {
        bool isRock = ElevationFunction?.ValueAt(x, y) > 0.7; 
        return (isRock ? "Natural rock" : TerrainData.DislayString(value.Terrain)) + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
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
        return GetTerrainColor(value.Terrain, value.SelectionIndex);
    }

    public Color GetTerrainColor(TerrainDef def, int selIdx)
    {
        return _terrainColors[selIdx % _terrainColors.Count];
    }
}
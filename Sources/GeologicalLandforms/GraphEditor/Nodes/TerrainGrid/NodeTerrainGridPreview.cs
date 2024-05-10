using System;
using MapPreview;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Grid/Preview", 320)]
public class NodeTerrainGridPreview : NodeDiscreteGridPreview<TerrainDef>
{
    public static readonly Color RockColor = GenColor.FromHex("36271C");

    public const string ID = "terrainGridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override IGridFunction<TerrainDef> Default => GridFunction.Of<TerrainDef>(null);

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
        var previewSize = PreviewSize;
        var previewBuffer = PreviewBuffer;

        var previewTransform = NodeGridPreview.GetPreviewTransform(PreviewTransformId);

        var supplier = SupplierOrFallback(InputKnob, Default);
        var elevSupplier = SupplierOrFallback(ElevationInputKnob, GridFunction.Zero);

        TerrainCanvas.PreviewScheduler.ScheduleTask(new PreviewTask(this, () =>
        {
            var previewFunction = PreviewFunction = supplier.ResetAndGet();
            var elevationFunction = ElevationFunction = elevSupplier.ResetAndGet();

            for (int x = 0; x < previewSize; x++)
            {
                for (int y = 0; y < previewSize; y++)
                {
                    var pos = previewTransform.PreviewToCanvasSpace(TerrainCanvas, new Vector2Int(x, y));
                    var value = previewFunction.ValueAt(pos.x, pos.y);
                    var elevation = elevationFunction.ValueAt(pos.x, pos.y);
                    var color = elevation > 0.7f ? RockColor : GetColor(value);
                    previewBuffer[y * previewSize + x] = color;
                }
            }
        }, () =>
        {
            if (PreviewTexture != null)
            {
                PreviewTexture.SetPixels(PreviewBuffer);
                PreviewTexture.Apply();
            }
        }));
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<TerrainDef>>>());
        ElevationOutputKnob.SetValue(ElevationInputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }

    protected override string MakeTooltip(TerrainDef value, double x, double y)
    {
        bool isRock = ElevationFunction?.ValueAt(x, y) > 0.7;
        var displayName = isRock ? "Natural rock" : TerrainFunctionConnection.Instance.DisplayName(value);
        return $"{displayName} ( {Math.Round(x, 0)} | {Math.Round(y, 0)} )";
    }

    protected override Color GetColor(TerrainDef value)
    {
        if (value == null) return Color.black;
        return TrueTerrainColors.TrueColors.TryGetValue(value.defName, out var color) ? color : Color.white;
    }
}

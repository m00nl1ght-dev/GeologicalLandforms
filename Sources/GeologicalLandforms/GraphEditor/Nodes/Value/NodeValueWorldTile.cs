using System;
using NodeEditorFramework;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/Tile Values", 1000)]
public class NodeValueWorldTile : NodeBase
{
    public const string ID = "valueWorldTile";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "World Tile";

    [ValueConnectionKnob("Biome", Direction.Out, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob BiomeOutputKnob;

    [ValueConnectionKnob("Hilliness", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob HillinessOutputKnob;

    [ValueConnectionKnob("Elevation", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob ElevationOutputKnob;

    [ValueConnectionKnob("Temperature", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TemperatureOutputKnob;

    [ValueConnectionKnob("Rainfall", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob RainfallOutputKnob;

    [ValueConnectionKnob("Topology Value", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TopologyValueOutputKnob;

    [ValueConnectionKnob("Cave Depth", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob CaveSystemDepthValueOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Biome", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        BiomeOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Hilliness", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        HillinessOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Elevation", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        ElevationOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Temperature", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TemperatureOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Rainfall", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RainfallOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Topology Value", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TopologyValueOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Cave Depth", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        CaveSystemDepthValueOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        ElevationOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Elevation));
        HillinessOutputKnob.SetValue<ISupplier<double>>(Supplier.Of(GetHillinessFactor(Landform.GeneratingTile.Hilliness)));
        TemperatureOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Temperature));
        RainfallOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Rainfall));
        BiomeOutputKnob.SetValue<ISupplier<BiomeData>>(Supplier.Of(new BiomeData(Landform.GeneratingTile.Biome)));
        TopologyValueOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.TopologyValue));
        CaveSystemDepthValueOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.DepthInCaveSystem));
        return true;
    }

    public static double GetHillinessFactor(Hilliness hilliness)
    {
        return hilliness switch
        {
            Hilliness.Flat => MapGenTuning.ElevationFactorFlat,
            Hilliness.SmallHills => MapGenTuning.ElevationFactorSmallHills,
            Hilliness.LargeHills => MapGenTuning.ElevationFactorLargeHills,
            Hilliness.Mountainous => MapGenTuning.ElevationFactorMountains,
            Hilliness.Impassable => MapGenTuning.ElevationFactorImpassableMountains,
            _ => 1
        };
    }
}

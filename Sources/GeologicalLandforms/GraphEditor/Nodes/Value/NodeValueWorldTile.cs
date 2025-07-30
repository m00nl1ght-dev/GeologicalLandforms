using System;
using NodeEditorFramework;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using TerrainGraph.Util;
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

    [ValueConnectionKnob("Topology Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TopologyAngleOutputKnob;

    [ValueConnectionKnob("Cave Depth", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob CaveSystemDepthValueOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        KnobLabelDouble(BiomeOutputKnob, "Biome");
        KnobLabelDouble(HillinessOutputKnob, "Hilliness");
        KnobLabelDouble(ElevationOutputKnob, "Elevation");
        KnobLabelDouble(TemperatureOutputKnob, "Temperature");
        KnobLabelDouble(RainfallOutputKnob, "Rainfall");
        KnobLabelDouble(TopologyValueOutputKnob, "Topo Value");
        KnobLabelDouble(TopologyAngleOutputKnob, "Topo Angle");
        KnobLabelDouble(CaveSystemDepthValueOutputKnob, "Cave Depth");

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        SetOutputValue(ElevationOutputKnob, (double) Landform.GeneratingTile.Elevation);
        SetOutputValue(HillinessOutputKnob, GetHillinessFactor(Landform.GeneratingTile.Hilliness));
        SetOutputValue(TemperatureOutputKnob, (double) Landform.GeneratingTile.Temperature);
        SetOutputValue(RainfallOutputKnob, (double) Landform.GeneratingTile.Rainfall);
        SetOutputValue(BiomeOutputKnob, Landform.GeneratingTile.Biome);
        SetOutputValue(TopologyValueOutputKnob, (double) Landform.GeneratingTile.TopologyValue);
        SetOutputValue(TopologyAngleOutputKnob, GetTopologyAngle(Landform.GeneratingTile));
        SetOutputValue(CaveSystemDepthValueOutputKnob, (double) Landform.GeneratingTile.DepthInCaveSystem);
        return true;
    }

    public static double GetTopologyAngle(IWorldTileInfo tile)
    {
        var angle = (double) tile.TopologyDirection.AsAngle;

        if (tile.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides)
        {
            angle += 45;
        }

        return angle.NormalizeDeg();
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

using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/Rivers & Roads", 1002)]
public class NodeValueRiversAndRoads : NodeBase
{
    public const string ID = "valueRiversAndRoads";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Rivers & Roads";

    [ValueConnectionKnob("Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob AngleOutputKnob;

    [ValueConnectionKnob("Offset", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OffsetOutputKnob;

    [ValueConnectionKnob("River Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob RiverWidthOutputKnob;

    [ValueConnectionKnob("Road Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob RoadWidthOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Angle", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        AngleOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Offset", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        OffsetOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("River Size", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RiverWidthOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Road Size", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RoadWidthOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        var mainRiver = Landform.GeneratingTile.MainRiver;
        var mainRoad = Landform.GeneratingTile.MainRoad;

        float angle = 0f;
        float offset = 0f;

        if (Landform.GeneratingTile is WorldTileInfo tile)
        {
            if (mainRiver != null)
            {
                // TODO if there is a river landform, use mean of inflow and outflow angle instead
                angle = tile.RiverAngle(WorldTileUtils.CalculateMainRiverAngle(tile.World.grid, tile.TileId));
                offset = WorldTileUtils.RiverPositionToOffset(tile.RiverPosition(0), angle);
            }
            else if (mainRoad != null)
            {
                angle = tile.World.grid.GetHeadingFromTo(tile.TileId, tile.Tile.LargestRoadLink().neighbor);
            }
        }

        AngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) angle));
        OffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) offset));
        RiverWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) mainRiver.WidthOnWorld()));
        RoadWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) mainRoad.WidthOnWorld()));

        return true;
    }
}

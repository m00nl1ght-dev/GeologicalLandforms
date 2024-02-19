using System;
using NodeEditorFramework;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/River Links", 1003)]
public class NodeValueRiverLinks : NodeBase
{
    public const string ID = "valueRiverLinks";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "River Links";

    [ValueConnectionKnob("Inflow Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InflowAngleOutputKnob;

    [ValueConnectionKnob("Inflow Offset", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InflowOffsetOutputKnob;

    [ValueConnectionKnob("Inflow Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InflowWidthOutputKnob;

    [ValueConnectionKnob("Tributary Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TributaryAngleOutputKnob;

    [ValueConnectionKnob("Tributary Offset", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TributaryOffsetOutputKnob;

    [ValueConnectionKnob("Tributary Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TributaryWidthOutputKnob;

    [ValueConnectionKnob("Outflow Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutflowAngleOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Inflow Angle", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InflowAngleOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Inflow Offset", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InflowOffsetOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Inflow Width", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InflowWidthOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Tributary Angle", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TributaryAngleOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Tributary Offset", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TributaryOffsetOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Tributary Width", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TributaryWidthOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Outflow Angle", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        OutflowAngleOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        float inflowAngle = 0f;
        float inflowOffset = 0f;
        float inflowWidth = 0f;
        float tributaryAngle = 0f;
        float tributaryOffset = 0f;
        float tributaryWidth = 0f;
        float outflowAngle = 0f;

        if (Landform.GeneratingTile is WorldTileInfo tile)
        {
            var riverLinks = tile.Tile.Rivers;

            if (riverLinks != null)
            {
                Tile.RiverLink inflow = default;
                Tile.RiverLink tributary = default;
                Tile.RiverLink outflow = default;

                foreach (var link in riverLinks)
                {
                    if (WorldTileUtils.IsRiverInflow(tile.World.grid, tile.TileId, link))
                    {
                        if (link.river.WidthOnWorld() > inflow.river.WidthOnWorld())
                        {
                            tributary = inflow;
                            inflow = link;
                        }
                        else if (link.river.WidthOnWorld() > tributary.river.WidthOnWorld())
                        {
                            tributary = link;
                        }
                    }
                    else
                    {
                        if (link.river.WidthOnWorld() > outflow.river.WidthOnWorld()) outflow = link;
                    }
                }

                if (inflow.river != null)
                {
                    inflowAngle = tile.World.grid.GetHeadingFromTo(inflow.neighbor, tile.TileId);
                    inflowOffset = WorldTileUtils.RiverPositionToOffset(tile.RiverPosition(0), inflowAngle);
                    inflowWidth = inflow.river.widthOnMap;
                }

                if (tributary.river != null)
                {
                    tributaryAngle = tile.World.grid.GetHeadingFromTo(tributary.neighbor, tile.TileId);
                    tributaryOffset = WorldTileUtils.RiverPositionToOffset(tile.RiverPosition(1), tributaryAngle);
                    tributaryWidth = tributary.river.widthOnMap;
                }

                if (outflow.river != null)
                {
                    outflowAngle = tile.RiverAngle(tile.World.grid.GetHeadingFromTo(tile.TileId, outflow.neighbor));
                }
            }
        }
        else if (Landform.GeneratingTile is EditorMockTileInfo mock)
        {
            inflowAngle = mock.RiverInflowAngle;
            inflowOffset = mock.RiverInflowOffset;
            inflowWidth = mock.RiverInflowWidth;
            tributaryAngle = mock.RiverTributaryAngle;
            tributaryOffset = mock.RiverTributaryOffset;
            tributaryWidth = mock.RiverTributaryWidth;
            outflowAngle = mock.RiverOutflowAngle;
        }

        InflowAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) inflowAngle));
        InflowOffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) inflowOffset));
        InflowWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) inflowWidth));
        TributaryAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) tributaryAngle));
        TributaryOffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) tributaryOffset));
        TributaryWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) tributaryWidth));
        OutflowAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) outflowAngle));

        return true;
    }
}

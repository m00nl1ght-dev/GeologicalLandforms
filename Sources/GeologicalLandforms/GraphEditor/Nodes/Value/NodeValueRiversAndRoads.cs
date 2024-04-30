using System;
using NodeEditorFramework;
using TerrainGraph;
using TerrainGraph.Util;
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
        var riverData = Landform.GeneratingTile.Rivers;
        var roadData = Landform.GeneratingTile.Roads;

        double angle = 0;
        double offset = 0;

        if (riverData.RiverOutflowWidth > 0)
        {
            angle = riverData.RiverOutflowAngle;
            offset = riverData.RiverInflowOffset;

            if (riverData.RiverInflowWidth > 0)
            {
                double diff = 180 + riverData.RiverInflowAngle - angle;
                angle += diff.NormalizeDeg() * 0.5;
            }
        }
        else if (riverData.RiverInflowWidth > 0)
        {
            angle = riverData.RiverInflowAngle;
        }
        else
        {
            angle = roadData.RoadPrimaryAngle;
        }

        AngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of(angle));
        OffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of(offset));

        RiverWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.MainRiver.WidthOnWorld()));
        RoadWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.MainRoad.WidthOnWorld()));

        return true;
    }
}

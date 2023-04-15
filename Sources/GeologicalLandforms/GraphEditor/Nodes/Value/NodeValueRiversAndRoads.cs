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

    [ValueConnectionKnob("Road Multiplier", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob RoadMultiplierOutputKnob;

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
        GUILayout.Label("River Width", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RiverWidthOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Road Factor", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RoadMultiplierOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        float riverWidth = Landform.GeneratingTile.MainRiver?.widthOnWorld ?? 0f;
        float mainRoadMultiplier = Landform.GeneratingTile.MainRoad?.movementCostMultiplier ?? 1f;

        float angle = 0f;
        float offset = 0f;
        
        if (Landform.GeneratingTile.MainRiver != null)
        {
            angle = Landform.GeneratingTile.MainRiverAngle;
            offset = PositionToOffset(Landform.GeneratingTile.MainRiverPosition, angle);
        }
        else if (Landform.GeneratingTile.MainRoad != null)
        {
            angle = Landform.GeneratingTile.MainRoadAngle;
        }

        AngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) angle));
        OffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) offset));
        RiverWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverWidth));
        RoadMultiplierOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) mainRoadMultiplier));
        return true;
    }

    private static float PositionToOffset(Vector3 position, float angle)
    {
        var pos = new Vector2(position.x - 0.5f, position.z - 0.5f);
        var vec = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
        var diff = pos - vec * Vector2.Dot(pos, vec);
        return Vector2.SignedAngle(vec, pos) > 0f ? -diff.magnitude : diff.magnitude;
    }
}

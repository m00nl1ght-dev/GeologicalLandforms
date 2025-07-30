using System;
using NodeEditorFramework;
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

    [ValueConnectionKnob("Tertiary Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TertiaryAngleOutputKnob;

    [ValueConnectionKnob("Tertiary Offset", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TertiaryOffsetOutputKnob;

    [ValueConnectionKnob("Tertiary Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TertiaryWidthOutputKnob;

    [ValueConnectionKnob("Outflow Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutflowAngleOutputKnob;

    [ValueConnectionKnob("Outflow Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutflowWidthOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        KnobLabelDouble(InflowAngleOutputKnob, "Inflow Angle");
        KnobLabelDouble(InflowOffsetOutputKnob, "Inflow Offset");
        KnobLabelDouble(InflowWidthOutputKnob, "Inflow Width");
        KnobLabelDouble(TributaryAngleOutputKnob, "Tributary Angle");
        KnobLabelDouble(TributaryOffsetOutputKnob, "Tributary Offset");
        KnobLabelDouble(TributaryWidthOutputKnob, "Tributary Width");
        KnobLabelDouble(TertiaryAngleOutputKnob, "Tertiary Angle");
        KnobLabelDouble(TertiaryOffsetOutputKnob, "Tertiary Offset");
        KnobLabelDouble(TertiaryWidthOutputKnob, "Tertiary Width");
        KnobLabelDouble(OutflowAngleOutputKnob, "Outflow Angle");
        KnobLabelDouble(OutflowWidthOutputKnob, "Outflow Width");

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        var riverData = Landform.GeneratingTile.Rivers;

        SetOutputValue(InflowAngleOutputKnob, (double) riverData.RiverInflowAngle);
        SetOutputValue(InflowOffsetOutputKnob, (double) riverData.RiverInflowOffset);
        SetOutputValue(InflowWidthOutputKnob, (double) riverData.RiverInflowWidth);
        SetOutputValue(TributaryAngleOutputKnob, (double) riverData.RiverTributaryAngle);
        SetOutputValue(TributaryOffsetOutputKnob, (double) riverData.RiverTributaryOffset);
        SetOutputValue(TributaryWidthOutputKnob, (double) riverData.RiverTributaryWidth);
        SetOutputValue(TertiaryAngleOutputKnob, (double) riverData.RiverTertiaryAngle);
        SetOutputValue(TertiaryOffsetOutputKnob, (double) riverData.RiverTertiaryOffset);
        SetOutputValue(TertiaryWidthOutputKnob, (double) riverData.RiverTertiaryWidth);
        SetOutputValue(OutflowAngleOutputKnob, (double) riverData.RiverOutflowAngle);
        SetOutputValue(OutflowWidthOutputKnob, (double) riverData.RiverOutflowWidth);

        return true;
    }
}

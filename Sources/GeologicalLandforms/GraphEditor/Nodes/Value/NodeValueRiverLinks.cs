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

    [ValueConnectionKnob("Outflow Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutflowAngleOutputKnob;

    [ValueConnectionKnob("Outflow Width", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutflowWidthOutputKnob;

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

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Outflow Width", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        OutflowWidthOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        var riverData = Landform.GeneratingTile.Rivers;

        InflowAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverInflowAngle));
        InflowOffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverInflowOffset));
        InflowWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverInflowWidth));
        TributaryAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverTributaryAngle));
        TributaryOffsetOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverTributaryOffset));
        TributaryWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverTributaryWidth));
        OutflowAngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverOutflowAngle));
        OutflowWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverData.RiverOutflowWidth));

        return true;
    }
}

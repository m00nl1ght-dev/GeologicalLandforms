using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Value/Rivers & Roads", 151)]
public class NodeValueRiversAndRoads : NodeBase
{
    public const string ID = "valueRiversAndRoads";
    public override string GetID => ID;
    
    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Rivers & Roads";
    
    [ValueConnectionKnob("Angle", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob AngleOutputKnob;
    
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
        if (Landform.GeneratingTile.MainRiver != null)
        {
            angle = Landform.GeneratingTile.MainRiverAngle;
        } 
        else if (Landform.GeneratingTile.MainRoad != null)
        {
            angle = Landform.GeneratingTile.MainRoadAngle;
        }
        
        AngleOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) angle));
        RiverWidthOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) riverWidth));
        RoadMultiplierOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) mainRoadMultiplier));
        return true;
    }
}
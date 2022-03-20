using System;
using NodeEditorFramework;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Value/World Tile", 150)]
public class NodeValueWorldTile : NodeBase
{
    public const string ID = "valueWorldTile";
    public override string GetID => ID;
    
    public override Vector2 MinSize => new(100, 10);

    public override string Title => "World Tile";
    
    [ValueConnectionKnob("Elevation", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob ElevationOutputKnob;
    
    [ValueConnectionKnob("Hilliness", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob HillinessOutputKnob;
    
    [ValueConnectionKnob("Temperature", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob TemperatureOutputKnob;
    
    [ValueConnectionKnob("Rainfall", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob RainfallOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Elevation", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        ElevationOutputKnob.SetPosition();
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Hilliness", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        HillinessOutputKnob.SetPosition();
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Temperature", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        TemperatureOutputKnob.SetPosition();
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Rainfall", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RainfallOutputKnob.SetPosition();
        
        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        ElevationOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Elevation));
        HillinessOutputKnob.SetValue<ISupplier<double>>(Supplier.Of(GetHillinessFactor(Landform.GeneratingTile.Hilliness)));
        TemperatureOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Temperature));
        RainfallOutputKnob.SetValue<ISupplier<double>>(Supplier.Of((double) Landform.GeneratingTile.Rainfall));
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
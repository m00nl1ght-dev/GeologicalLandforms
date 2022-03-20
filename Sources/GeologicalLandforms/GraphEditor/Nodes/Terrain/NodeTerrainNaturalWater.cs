using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Natural Water", 311)]
public class NodeTerrainNaturalWater : NodeBase
{
    public const string ID = "terrainNaturalWater";
    public override string GetID => ID;
    
    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Natural Water";
    
    [ValueConnectionKnob("Shallow", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob ShallowOutputKnob;

    [ValueConnectionKnob("Deep", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob DeepOutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Deep", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        DeepOutputKnob.SetPosition();
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Shallow", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        ShallowOutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        TerrainDef shallow = Landform.GeneratingTile.HasOcean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow;
        TerrainDef deep = Landform.GeneratingTile.HasOcean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep;
        ShallowOutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(new TerrainData(shallow, 2)));
        DeepOutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(new TerrainData(deep)));
        return true;
    }
}
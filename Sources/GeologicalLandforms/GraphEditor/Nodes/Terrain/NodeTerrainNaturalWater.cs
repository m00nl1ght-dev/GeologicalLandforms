using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;
using static GeologicalLandforms.IWorldTileInfo;
using static GeologicalLandforms.IWorldTileInfo.CoastType;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Natural Water", 312)]
public class NodeTerrainNaturalWater : NodeBase
{
    public const string ID = "terrainNaturalWater";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Natural Water";

    [ValueConnectionKnob("Deep", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob DeepOutputKnob;

    [ValueConnectionKnob("Shallow", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob ShallowOutputKnob;

    [ValueConnectionKnob("Beach", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob BeachOutputKnob;

    public NodeGridRotateToMapSides.MapSide MapSide;

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

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Beach", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        BeachOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);

        if (GUILayout.Button(MapSide.ToString(), GUI.skin.box))
        {
            Dropdown<NodeGridRotateToMapSides.MapSide>(s =>
            {
                MapSide = s;
                canvas.OnNodeChange(this);
            });
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public static event Action<Data> OnCalculate;

    public override bool Calculate()
    {
        var angle = Landform.GeneratingTile.TopologyDirection.AsAngle + (float) NodeGridRotateToMapSides.MapSideToWorldAngle(MapSide);
        var data = new Data(Landform.GeneratingTile.Biome, Landform.GeneratingTile.Coast[Rot4.FromAngleFlat(angle)]);

        OnCalculate?.Invoke(data);

        BeachOutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(new TerrainData(data.Beach, 3)));
        ShallowOutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(new TerrainData(data.Shallow, 2)));
        DeepOutputKnob.SetValue<ISupplier<TerrainData>>(Supplier.Of(new TerrainData(data.Deep)));
        return true;
    }

    public class Data
    {
        public readonly CoastType CoastType;
        public readonly BiomeDef Biome;

        public TerrainDef Beach;
        public TerrainDef Shallow;
        public TerrainDef Deep;

        public Data(BiomeDef biome, CoastType coastType)
        {
            Biome = biome;
            CoastType = coastType;
            Beach = biome.Properties().beachTerrain ?? TerrainDefOf.Sand;
            Shallow = coastType == Ocean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow;
            Deep = coastType == Ocean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep;
        }
    }
}

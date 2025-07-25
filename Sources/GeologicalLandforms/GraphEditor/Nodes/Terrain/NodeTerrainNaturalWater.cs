using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;
using static GeologicalLandforms.CoastType;

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

    [ValueConnectionKnob("RiverDeep", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob RiverDeepOutputKnob;

    [ValueConnectionKnob("RiverShallow", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob RiverShallowOutputKnob;

    [ValueConnectionKnob("Riverbank", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob RiverbankOutputKnob;

    public NodeGridRotateToMapSides.MapSide MapSide;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Deep Water", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        DeepOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Shallow Water", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        ShallowOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Beach", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        BeachOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Deep River", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RiverDeepOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Shallow River", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RiverShallowOutputKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Riverbank", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        RiverbankOutputKnob.SetPosition();

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

        BeachOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.Beach));
        ShallowOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.Shallow));
        DeepOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.Deep));
        RiverbankOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.Riverbank));
        RiverShallowOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.RiverShallow));
        RiverDeepOutputKnob.SetValue<ISupplier<TerrainDef>>(Supplier.Of(data.RiverDeep));
        return true;
    }

    public class Data
    {
        public readonly CoastType CoastType;
        public readonly BiomeDef Biome;

        public TerrainDef Beach;
        public TerrainDef Shallow;
        public TerrainDef Deep;
        public TerrainDef Riverbank;
        public TerrainDef RiverShallow;
        public TerrainDef RiverDeep;

        public Data(BiomeDef biome, CoastType coastType)
        {
            Biome = biome;
            CoastType = coastType;

            #if RW_1_6_OR_GREATER

            if (coastType == Ocean)
            {
                Beach = biome.coastalBeachTerrain ?? TerrainDefOf.Sand;
                Shallow = biome.oceanShallowTerrain ?? TerrainDefOf.WaterOceanShallow;
                Deep = biome.oceanDeepTerrain ?? TerrainDefOf.WaterOceanDeep;
            }
            else
            {
                Beach = biome.lakeBeachTerrain ?? TerrainDefOf.Sand;
                Shallow = biome.waterShallowTerrain ?? TerrainDefOf.WaterShallow;
                Deep = biome.waterDeepTerrain ?? TerrainDefOf.WaterDeep;
            }

            Riverbank = biome.riverbankTerrain ?? TerrainDefOf.Riverbank;
            RiverShallow = biome.waterMovingShallowTerrain ?? TerrainDefOf.WaterMovingShallow;
            RiverDeep = biome.waterMovingChestDeepTerrain ?? TerrainDefOf.WaterMovingChestDeep;

            #else

            Beach = biome.Properties().beachTerrain ?? TerrainDefOf.Sand;
            Shallow = coastType == Ocean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow;
            Deep = coastType == Ocean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep;
            Riverbank = TerrainDefOf.Soil;
            RiverShallow = TerrainDefOf.WaterMovingShallow;
            RiverDeep = TerrainDefOf.WaterMovingChestDeep;

            #endif
        }
    }
}

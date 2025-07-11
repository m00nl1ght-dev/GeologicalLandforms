using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using LunarFramework.GUI;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class EditorMockTileInfo : IWorldTileInfo
{
    public IReadOnlyList<Landform> Landforms => LandformsList;
    public List<Landform> LandformsList { get; set; }

    public IReadOnlyList<BorderingBiome> BorderingBiomes => BorderingBiomesList;
    public List<BorderingBiome> BorderingBiomesList { get; set; }

    public IReadOnlyList<BiomeVariantDef> BiomeVariants => BiomeVariantsList;
    public List<BiomeVariantDef> BiomeVariantsList { get; set; }

    public Topology Topology => Topology.Any;
    public float TopologyValue { get; set; }
    public Rot4 TopologyDirection { get; set; } = Rot4.North;
    public byte DepthInCaveSystem { get; set; }
    public StructRot4<CoastType> Coast { get; set; }
    public RiverType RiverType { get; set; }

    public MapParent WorldObject => null;
    public BiomeDef Biome { get; set; } = BiomeDefOf.TemperateForest;

    public Hilliness Hilliness { get; set; } = Hilliness.Flat;
    public float Elevation { get; set; } = 1000f;
    public float Temperature { get; set; } = 20f;
    public float Rainfall { get; set; } = 1000f;
    public float Swampiness { get; set; } = 0f;
    public float Pollution { get; set; } = 0f;
    public bool HasCaves { get; set; } = true;

    public RiverDef MainRiver { get; set; }
    public RoadDef MainRoad { get; set; }

    public IRiverData Rivers { get; set; } = new RiverData();
    public IRoadData Roads { get; set; } = new RoadData();

    #if RW_1_6_OR_GREATER
    public Landmark Landmark { get; set; }
    #endif

    public Vector3 PosInWorld { get; set; }

    public int StableSeed(int salt) => Gen.HashCombineInt(this.GetHashCode(), salt);

    public void DoEditorGUI(LayoutRect layout, Action<object> onChange)
    {
        LunarGUI.Label(layout, "Simulated tile properties");
        LunarGUI.SeparatorLine(layout, 3f);

        layout.Abs(5f);

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.5f), "Biome");
        LunarGUI.Dropdown(layout.Rel(-1), Biome, DefDatabase<BiomeDef>.AllDefs, v => onChange(Biome = v), v => v.LabelCap);
        layout.End();

        var hillinessFiltered = typeof(Hilliness).GetEnumValues().Cast<Hilliness>().Where(v => v != Hilliness.Undefined);

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.5f), "Hilliness");
        LunarGUI.Dropdown(layout.Rel(-1), Hilliness, hillinessFiltered, v => onChange(Hilliness = v), v => v.GetLabelCap());
        layout.End();

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.5f), "Direction");
        List<Rot4> rots = [Rot4.North, Rot4.East, Rot4.South, Rot4.West];
        LunarGUI.Dropdown(layout.Rel(-1), TopologyDirection, rots, v => onChange(TopologyDirection = v), v => v.ToStringHuman());
        layout.End();
    }

    public struct RiverData : IRiverData
    {
        public RiverData() {}

        public float RiverInflowAngle { get; set; } = 0f;
        public float RiverInflowOffset { get; set; } = 0f;
        public float RiverInflowWidth { get; set; } = 20f;
        public float RiverTributaryAngle { get; set; } = -45f;
        public float RiverTributaryOffset { get; set; } = 0.1f;
        public float RiverTributaryWidth { get; set; } = 10f;
        public float RiverTertiaryWidth { get; set; } = 0f;
        public float RiverTertiaryAngle { get; set; } = 0f;
        public float RiverTertiaryOffset { get; set; } = 0f;
        public float RiverOutflowAngle { get; set; } = 0f;
        public float RiverOutflowWidth { get; set; } = 20f;
    }

    public struct RoadData : IRoadData
    {
        public RoadData() {}

        public float RoadPrimaryAngle { get; set; } = 60f;
        public float RoadSecondaryAngle { get; set; } = -120f;
    }
}

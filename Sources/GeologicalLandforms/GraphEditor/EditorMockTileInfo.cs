using System.Collections.Generic;
using GeologicalLandforms.Defs;
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

    public Vector3 PosInWorld { get; set; }

    public int StableSeed(int salt) => Gen.HashCombineInt(this.GetHashCode(), salt);

    public struct RiverData : IRiverData
    {
        public RiverData() {}

        public float RiverInflowAngle { get; set; } = 30f;
        public float RiverInflowOffset { get; set; } = 0.1f;
        public float RiverInflowWidth { get; set; } = 20f;
        public float RiverTributaryAngle { get; set; } = -80f;
        public float RiverTributaryOffset { get; set; } = -0.1f;
        public float RiverTributaryWidth { get; set; } = 10f;
        public float RiverOutflowAngle { get; set; } = -45f;
        public float RiverOutflowWidth { get; set; } = 1f;
    }

    public struct RoadData : IRoadData
    {
        public RoadData() {}

        public float RoadPrimaryAngle { get; set; } = 60f;
        public float RoadSecondaryAngle { get; set; } = -120f;
    }
}

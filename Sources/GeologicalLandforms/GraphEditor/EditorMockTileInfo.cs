using System.Collections.Generic;
using GeologicalLandforms.Defs;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;
using static GeologicalLandforms.IWorldTileInfo;

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
    public RiverType River { get; set; }

    public MapParent WorldObject => null;
    public BiomeDef Biome { get; set; } = BiomeDefOf.TemperateForest;

    public Hilliness Hilliness { get; set; } = Hilliness.Flat;
    public float Elevation { get; set; } = 1000f;
    public float Temperature { get; set; } = 20f;
    public float Rainfall { get; set; } = 1000f;
    public float Swampiness { get; set; } = 0f;

    public RiverDef MainRiver { get; set; }
    public RoadDef MainRoad { get; set; }

    public float RiverInflowAngle { get; set; } = 30f;
    public float RiverInflowOffset { get; set; } = 0.1f;
    public float RiverInflowWidth { get; set; } = 20f;
    public float RiverTributaryAngle { get; set; } = -80f;
    public float RiverTributaryOffset { get; set; } = -0.1f;
    public float RiverTributaryWidth { get; set; } = 10f;
    public float RiverOutflowAngle { get; set; } = -45f;
}

using System.Collections.Generic;
using GeologicalLandforms.Defs;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class EditorMockTileInfo : IWorldTileInfo
{
    public IReadOnlyList<Landform> Landforms => LandformsList;
    public List<Landform> LandformsList { get; set; }

    public IReadOnlyList<IWorldTileInfo.BorderingBiome> BorderingBiomes => BorderingBiomesList;
    public List<IWorldTileInfo.BorderingBiome> BorderingBiomesList { get; set; }

    public IReadOnlyList<BiomeVariantDef> BiomeVariants => BiomeVariantsList;
    public List<BiomeVariantDef> BiomeVariantsList { get; set; }

    public Topology Topology => Topology.Any;
    public float TopologyValue { get; set; }
    public Rot4 TopologyDirection { get; set; } = Rot4.North;
    public byte DepthInCaveSystem { get; set; }
    public StructRot4<IWorldTileInfo.CoastType> Coast { get; set; }

    public MapParent WorldObject => null;
    public BiomeDef Biome { get; set; } = BiomeDefOf.TemperateForest;

    public Hilliness Hilliness { get; set; } = Hilliness.Flat;
    public float Elevation { get; set; } = 1000f;
    public float Temperature { get; set; } = 20f;
    public float Rainfall { get; set; } = 1000f;
    public float Swampiness { get; set; } = 0f;

    public RiverDef MainRiver { get; set; } = null;
    public float MainRiverAngle { get; set; } = 0f;

    public RoadDef MainRoad { get; set; } = null;
    public float MainRoadAngle { get; set; } = 0f;

    public float ExpectedMapSize => 250f;
}

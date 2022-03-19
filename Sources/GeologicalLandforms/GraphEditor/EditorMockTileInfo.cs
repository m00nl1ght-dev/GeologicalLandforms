using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class EditorMockTileInfo : IWorldTileInfo
{
    public EditorMockTileInfo(Landform landform)
    {
        Landform = landform;
    }

    public Landform Landform { get; }
    public Topology Topology => Topology.Any;
    public Rot4 LandformDirection { get; set; } = Rot4.North;

    public MapParent WorldObject => null;
    public BiomeDef Biome { get; set; } = BiomeDefOf.TemperateForest;

    public bool HasCaves { get; set; } = true;
    public bool HasOcean { get; set; } = true;

    public Hilliness Hilliness { get; set; } = Hilliness.Flat;
    public float Elevation { get; set; } = 1000f;
    public float Temperature { get; set; } = 20f;
    public float Rainfall { get; set; } = 1000f;
    public float Swampiness { get; set; } = 0f;

    public RiverDef MainRiver { get; set; } = null;
    public float MainRiverAngle { get; set; } = 0f;

    public RoadDef MainRoad { get; set; } = null;
    public float MainRoadAngle { get; set; } = 0f;
}
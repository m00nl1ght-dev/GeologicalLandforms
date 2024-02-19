using System.Collections.Generic;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public interface IWorldTileInfo
{
    public IReadOnlyList<Landform> Landforms { get; }
    public IReadOnlyList<BorderingBiome> BorderingBiomes { get; }
    public IReadOnlyList<BiomeVariantDef> BiomeVariants { get; }

    public Topology Topology { get; }
    public float TopologyValue { get; }
    public Rot4 TopologyDirection { get; }
    public byte DepthInCaveSystem { get; }
    public StructRot4<CoastType> Coast { get; }
    public RiverType River { get; }

    public MapParent WorldObject { get; }
    public BiomeDef Biome { get; }

    public Hilliness Hilliness { get; }
    public float Elevation { get; }
    public float Temperature { get; }
    public float Rainfall { get; }
    public float Swampiness { get; }

    public RiverDef MainRiver { get; }
    public RoadDef MainRoad { get; }

    public readonly struct BorderingBiome
    {
        public readonly BiomeDef Biome;
        public readonly float Angle;

        public BorderingBiome(BiomeDef biome, float angle)
        {
            Biome = biome;
            Angle = angle;
        }
    }

    public enum CoastType : byte
    {
        None,
        Lake,
        Ocean
    }

    public enum RiverType : byte
    {
        None,
        Source,
        Normal,
        Confluence,
        Delta
    }
}

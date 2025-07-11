using System.Collections.Generic;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class WorldTileInfoPrimer : WorldTileInfo
{
    public new IReadOnlyList<Landform> Landforms
    {
        get => base.Landforms;
        set => base.Landforms = value;
    }

    public new IReadOnlyList<BorderingBiome> BorderingBiomes
    {
        get => base.BorderingBiomes;
        set => base.BorderingBiomes = value;
    }

    public new IReadOnlyList<BiomeVariantDef> BiomeVariants
    {
        get => base.BiomeVariants;
        set => base.BiomeVariants = value;
    }

    public new Topology Topology
    {
        get => base.Topology;
        set => base.Topology = value;
    }

    public new float TopologyValue
    {
        get => base.TopologyValue;
        set => base.TopologyValue = value;
    }

    public new Rot4 TopologyDirection
    {
        get => base.TopologyDirection;
        set => base.TopologyDirection = value;
    }

    public new StructRot4<CoastType> Coast
    {
        get => base.Coast;
        set => base.Coast = value;
    }

    #if RW_1_6_OR_GREATER
    public WorldTileInfoPrimer(int tileId, SurfaceTile tile, World world) : base(tileId, tile, world) { }
    #else
    public WorldTileInfoPrimer(int tileId, Tile tile, World world) : base(tileId, tile, world) { }
    #endif

    public WorldTileInfoPrimer(WorldTileInfo other) : base(other.TileId, other.Tile, other.World)
    {
        Landforms = other.Landforms == null ? null : [..other.Landforms];
        BorderingBiomes = other.BorderingBiomes == null ? null : [..other.BorderingBiomes];
        BiomeVariants = other.BiomeVariants == null ? null : [..other.BiomeVariants];

        Topology = other.Topology;
        TopologyValue = other.TopologyValue;
        TopologyDirection = other.TopologyDirection;

        Coast = other.Coast;
        RiverType = other.RiverType;
    }
}

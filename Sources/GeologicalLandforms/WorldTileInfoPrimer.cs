using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld.Planet;
using Verse;
using static GeologicalLandforms.IWorldTileInfo;

namespace GeologicalLandforms;

public class WorldTileInfoPrimer : WorldTileInfo
{
    public new IReadOnlyList<Landform> Landforms { get => base.Landforms; set => base.Landforms = value; }
    public new IReadOnlyList<BorderingBiome> BorderingBiomes { get => base.BorderingBiomes; set => base.BorderingBiomes = value; }
    
    public new Topology Topology { get => base.Topology; set => base.Topology = value; }
    public new Rot4 LandformDirection { get => base.LandformDirection; set => base.LandformDirection = value; }
    
    public new StructRot4<CoastType> Coast { get => base.Coast; set => base.Coast = value; }
    
    public WorldTileInfoPrimer(int tileId, Tile tile, World world) : base(tileId, tile, world) {}
}
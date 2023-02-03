using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public readonly struct XmlContext
{
    public readonly int TileId;
    public readonly Tile Tile;

    public readonly WorldTileInfo TileInfo;

    public readonly Map Map;
    public readonly IntVec3 MapCell;

    public XmlContext(int tileId, Tile tile)
    {
        TileId = tileId;
        Tile = tile;
    }

    public XmlContext(WorldTileInfo tileInfo)
    {
        TileInfo = tileInfo;
        TileId = tileInfo.TileId;
        Tile = tileInfo.Tile;
    }

    public XmlContext(WorldTileInfo tileInfo, Map map, IntVec3 mapCell)
    {
        TileInfo = tileInfo;
        TileId = tileInfo.TileId;
        Tile = tileInfo.Tile;
        Map = map;
        MapCell = mapCell;
    }
}

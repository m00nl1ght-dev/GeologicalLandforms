using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public interface ICtxEarlyTile
{
    public int TileId { get; }
    public Tile Tile { get; }
    public World World { get; }
}

public readonly struct CtxEarlyTile : ICtxEarlyTile
{
    public int TileId { get; }
    public Tile Tile { get; }
    public World World { get; }

    public CtxEarlyTile(int tileId, Tile tile, World world)
    {
        TileId = tileId;
        Tile = tile;
        World = world;
    }
}

public interface ICtxTile : ICtxEarlyTile
{
    public WorldTileInfo TileInfo { get; }
}

public readonly struct CtxTile : ICtxTile
{
    public int TileId => TileInfo.TileId;
    public Tile Tile => TileInfo.Tile;
    public World World => TileInfo.World;

    public WorldTileInfo TileInfo { get; }

    public CtxTile(WorldTileInfo tileInfo)
    {
        TileInfo = tileInfo;
    }
}

public interface ICtxMapCell : ICtxTile
{
    public Map Map { get; }
    public IntVec3 MapCell { get; }
}

public readonly struct CtxMapCell : ICtxMapCell
{
    public int TileId => TileInfo.TileId;
    public Tile Tile => TileInfo.Tile;
    public World World => TileInfo.World;

    public WorldTileInfo TileInfo { get; }

    public Map Map { get; }
    public IntVec3 MapCell { get; }

    public CtxMapCell(WorldTileInfo tileInfo, Map map, IntVec3 mapCell)
    {
        TileInfo = tileInfo;
        Map = map;
        MapCell = mapCell;
    }
}

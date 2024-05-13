using GeologicalLandforms.GraphEditor;
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

public interface ICtxTile
{
    public IWorldTileInfo TileInfo { get; }
}

public readonly struct CtxTile : ICtxTile
{
    public IWorldTileInfo TileInfo { get; }

    public CtxTile(IWorldTileInfo tileInfo)
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
    public IWorldTileInfo TileInfo { get; }

    public Map Map { get; }
    public IntVec3 MapCell { get; }

    public CtxMapCell(IWorldTileInfo tileInfo, Map map, IntVec3 mapCell)
    {
        TileInfo = tileInfo;
        Map = map;
        MapCell = mapCell;
    }
}

public interface ICtxMapGenCell : ICtxMapCell;

public readonly struct CtxMapGenCell : ICtxMapGenCell
{
    public IWorldTileInfo TileInfo => Landform.GeneratingTile;
    public Map Map => MapGenerator.mapBeingGenerated;

    public IntVec3 MapCell { get; }

    public CtxMapGenCell(IntVec3 mapCell)
    {
        MapCell = mapCell;
    }
}

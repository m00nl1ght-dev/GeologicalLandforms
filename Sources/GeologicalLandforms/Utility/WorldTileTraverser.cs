using System;
using System.Collections.Generic;
using RimWorld.Planet;

namespace GeologicalLandforms;

public class WorldTileTraverser
{
    private bool _reentryFlag;

    private readonly Queue<QueuedTile> _queue = new();
    private readonly List<int> _visited = [];

    public delegate TraverseAction Processor(int tileId, int distance);

    public bool Traverse(WorldGrid worldGrid, int rootTile, int maxDistance, Processor processor)
    {
        return Traverse(worldGrid, rootTile, maxDistance, out _, out _, processor);
    }

    public bool Traverse(
        WorldGrid worldGrid,
        int rootTile,
        int maxDistance,
        out int resultTile,
        out int resultDistance,
        Processor processor)
    {
        if (_reentryFlag) throw new Exception("Nesting violation for WorldTileTraverser detected");
        _reentryFlag = true;

        _visited.Clear();
        _queue.Clear();

        var nbValues = worldGrid.ExtNbValues();
        var nbOffsets = worldGrid.ExtNbOffsets();

        _visited.Add(rootTile);
        _queue.Enqueue(new QueuedTile(rootTile, 0));

        while (_queue.Count > 0)
        {
            var tile = _queue.Dequeue();
            var tileId = tile.TileId;
            var nbDist = tile.Distance + 1;

            int nBound = tileId + 1 < nbOffsets.Length() ? nbOffsets[tileId + 1] : nbValues.Length();
            for (int nbIdx = nbOffsets[tileId]; nbIdx < nBound; nbIdx++)
            {
                int nb = nbValues[nbIdx];
                if (!_visited.Contains(nb))
                {
                    _visited.Add(nb);

                    var action = processor(nb, nbDist);

                    if (action == TraverseAction.Stop)
                    {
                        resultTile = tileId;
                        resultDistance = tile.Distance + 1;
                        _reentryFlag = false;
                        return true;
                    }

                    if (action == TraverseAction.Pass && nbDist < maxDistance)
                    {
                        _queue.Enqueue(new QueuedTile(nb, tile.Distance + 1));
                    }
                }
            }
        }

        resultDistance = maxDistance;
        resultTile = -1;

        _reentryFlag = false;
        return false;
    }

    private readonly struct QueuedTile
    {
        public readonly int TileId;
        public readonly int Distance;

        public QueuedTile(int tileId, int distance)
        {
            TileId = tileId;
            Distance = distance;
        }
    }

    public enum TraverseAction
    {
        Ignore,
        Pass,
        Stop
    }
}

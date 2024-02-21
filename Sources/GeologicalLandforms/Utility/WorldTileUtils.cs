using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace GeologicalLandforms;

public static class WorldTileUtils
{
    public static float CalculateMainRiverAngle(WorldGrid grid, int tile)
    {
        var riverLinks = grid[tile].Rivers;
        if (riverLinks == null) return 0f;

        var link = riverLinks.Where(r => !IsRiverInflow(grid, tile, r))
            .OrderByDescending(r => r.river.WidthOnWorld())
            .FirstOrDefault();

        if (link.river == null) link = grid[tile].LargestRiverLink();
        return grid.GetHeadingFromTo(tile, link.neighbor);
    }

    public static bool IsRiverInflow(WorldGrid grid, int tile, Tile.RiverLink link, int searchLimit = 200)
    {
        if (searchLimit <= 0) return false;
        if (grid[link.neighbor].WaterCovered) return false;

        var linkWidth = link.river.WidthOnWorld();
        var nextLinks = grid[link.neighbor].Rivers;

        foreach (var nextLink in nextLinks)
        {
            if (nextLink.neighbor != tile)
            {
                var nextLinkWidth = nextLink.river.WidthOnWorld();
                if (nextLinkWidth > linkWidth) return false;
                if (nextLinkWidth < linkWidth) continue;
                if (!IsRiverInflow(grid, link.neighbor, nextLink, searchLimit - 1)) return false;
            }
        }

        return true;
    }

    public static float RiverPositionToOffset(Vector3 position, float angle)
    {
        var pos = new Vector2(position.x - 0.5f, position.z - 0.5f);
        var vec = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
        var diff = pos - vec * Vector2.Dot(pos, vec);
        return Vector2.SignedAngle(vec, pos) > 0f ? -diff.magnitude : diff.magnitude;
    }

    public static CoastType CoastTypeFromTile(Tile tile)
    {
        if (tile.biome == BiomeDefOf.Ocean) return CoastType.Ocean;
        if (tile.biome == BiomeDefOf.Lake) return CoastType.Lake;
        if (tile.WaterCovered && tile.biome.Properties().isWaterCovered) return CoastType.Ocean;
        return CoastType.None;
    }

    public static CoastType CombineCoastTypes(CoastType a, CoastType b)
    {
        return (CoastType) Math.Max((int) a, (int) b);
    }
}

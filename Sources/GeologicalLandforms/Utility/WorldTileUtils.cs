using System;
using System.Linq;
using GeologicalLandforms.Patches;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public static class WorldTileUtils
{
    public static int StableWorldSeed => Patch_RimWorld_World.LastKnownInitialWorldSeed;

    public static float Longitude(Vector3 pos) => Mathf.Atan2(pos.x, -pos.z) * 57.29578f;
    public static float Latitude(Vector3 pos) => Mathf.Asin(pos.y / 100f) * 57.29578f;

    public static IWorldTileInfo SelectedWorldTile => Find.World?.UI?.selector is { selectedTile: >= 0 } selector
        ? WorldTileInfo.Get(selector.selectedTile) : null;

    public static IWorldTileInfo CurrentWorldTile => Find.CurrentMap != null
        ? WorldTileInfo.Get(Find.CurrentMap) : SelectedWorldTile;

    public static int StableSeedForTile(int tileId, int salt = 0)
    {
        var tileSeed = Gen.HashCombineInt(StableWorldSeed, tileId);
        return salt != 0 ? Gen.HashCombineInt(tileSeed, salt) : tileSeed;
    }

    public static float DistanceToNearestWorldObject(World world, Vector3 pos, WorldObjectDef def)
    {
        return (
            from worldObject in world.worldObjects.AllWorldObjects
            where worldObject.def == def && worldObject.Tile >= 0
            select world.grid.GetTileCenter(worldObject.Tile) into tileCenter
            select world.grid.ApproxDistanceInTiles(GenMath.SphericalDistance(pos.normalized, tileCenter.normalized))
        ).Prepend(99999f).Min();
    }

    public static bool IsRiverInflow(WorldGrid grid, int tile, Tile.RiverLink link, int searchLimit = 200)
    {
        if (searchLimit <= 0) return false;
        if (grid[link.neighbor].WaterCovered) return false;

        var linkWidth = link.river.WidthOnWorld();
        var nextLinks = grid[link.neighbor].potentialRivers;

        if (nextLinks != null)
        {
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
        }

        return true;
    }

    public static float RiverAngleForTile(IWorldTileInfo tile, float baseAngle)
    {
        var topoAngle = tile.TopologyDirection.AsAngle;

        if (tile.Topology is Topology.CoastOneSide or Topology.CliffAndCoast)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle - 30f) > 0f) return topoAngle - 30f;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 30f) < 0f) return topoAngle + 30f;
        }
        else if (tile.Topology == Topology.CoastTwoSides)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle) > 0f) return topoAngle;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 90f) < 0f) return topoAngle + 90f;
        }
        else if (tile.Topology == Topology.CoastThreeSides)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle - 15f) > 0f) return topoAngle - 15f;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 15f) < 0f) return topoAngle + 15f;
        }

        return baseAngle;
    }

    public static Vector3 RiverPositionForTile(IWorldTileInfo tile, int salt)
    {
        var x = Rand.RangeSeeded(0.3f, 0.7f, tile.StableSeed(9332 + salt));
        var z = Rand.RangeSeeded(0.3f, 0.7f, tile.StableSeed(2750 + salt));
        return new Vector3(x, 0f, z);
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

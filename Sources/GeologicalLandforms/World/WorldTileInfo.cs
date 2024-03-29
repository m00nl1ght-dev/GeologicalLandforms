using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static Verse.RotationDirection;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery

namespace GeologicalLandforms;

public class WorldTileInfo : IWorldTileInfo
{
    public readonly int TileId;
    public readonly Tile Tile;
    public readonly World World;

    public IReadOnlyList<Landform> Landforms { get; protected set; }
    public bool HasLandforms => Landforms?.Count > 0;

    public IReadOnlyList<BorderingBiome> BorderingBiomes { get; protected set; }
    public bool HasBorderingBiomes => BorderingBiomes?.Count > 0;

    public IReadOnlyList<BiomeVariantDef> BiomeVariants { get; protected set; }
    public bool HasBiomeVariants => BiomeVariants?.Count > 0;

    public Topology Topology { get; protected set; }
    public float TopologyValue { get; protected set; }
    public Rot4 TopologyDirection { get; protected set; }
    public StructRot4<CoastType> Coast { get; protected set; }
    public RiverType River { get; protected set; }

    public MapParent WorldObject => World.worldObjects?.MapParentAt(TileId);
    public BiomeDef Biome => Tile.biome;

    public Hilliness Hilliness => Tile.hilliness;
    public float Elevation => Tile.elevation;
    public float Temperature => Tile.temperature;
    public float Rainfall => Tile.rainfall;
    public float Swampiness => Tile.swampiness;
    public bool HasCaves => World.HasCaves(TileId);

    public RiverDef MainRiver => Tile.LargestRiverLink().river;
    public RoadDef MainRoad => Tile.LargestRoadLink().road;

    public byte DepthInCaveSystem => World.LandformData()?.GetCaveSystemDepthAt(TileId) ?? 0;

    public int MakeSeed(int salt) => Gen.HashCombineInt(Patch_RimWorld_World.StableSeedForTile(TileId), salt);

    protected WorldTileInfo(int tileId, Tile tile, World world)
    {
        TileId = tileId;
        Tile = tile;
        World = world;
    }

    private static WorldTileInfoPrimer[] _cache;
    private static int _validCacheVersion = 1;

    private int _cacheVersion;

    public static WorldTileInfo Get(int tileId, bool allowFromCache = true)
    {
        try
        {
            var cache = _cache;
            var validVersion = _validCacheVersion;

            var world = Find.World;
            var canUseCache = cache != null && tileId < cache.Length;

            if (canUseCache && allowFromCache)
            {
                var match = cache[tileId];
                if (match != null && match._cacheVersion == validVersion && match.World == world) return match;
            }

            var info = new WorldTileInfoPrimer(tileId, world.grid[tileId], world);
            var props = info.Biome.Properties();
            var data = world.LandformData();

            if (_tsc_waterTiles == null)
            {
                _tsc_waterTiles = new(6);
                _tsc_landTiles = new(6);
                _tsc_cliffTiles = new(6);
                _tsc_nonCliffTiles = new(6);
                _tsc_caveSystemTiles = new(6);
                _tsc_eligible = new(50);
                _tsc_ids = new(5);
            }

            DetermineTopology(info, data, props);

            if (data != null && data.TryGet(tileId, out var tileData))
            {
                tileData.Apply(info);
            }
            else
            {
                DetermineLandforms(info, props);
                DetermineBiomeVariants(info);
            }

            GeologicalLandformsAPI.ApplyWorldTileInfoHook(info);

            if (canUseCache)
            {
                info._cacheVersion = validVersion;
                cache[tileId] = info;
            }

            return info;
        }
        catch (ThreadAbortException) { throw; }
        catch (Exception e)
        {
            InvalidateCache();
            GeologicalLandformsAPI.Logger.Error($"Failed to get info for world tile {tileId}", e);
            return new WorldTileInfo(tileId, new Tile { biome = BiomeDefOf.Ocean, elevation = 0f }, Find.World);
        }
    }

    public static void InvalidateCache()
    {
        unchecked { _validCacheVersion++; }
    }

    public static void CreateNewCache()
    {
        InvalidateCache();
        var grid = Find.WorldGrid;
        _cache = grid == null ? null : new WorldTileInfoPrimer[grid.TilesCount];
    }

    public static void RemoveCache()
    {
        InvalidateCache();
        _cache = null;
    }

    [ThreadStatic]
    private static List<string> _tsc_ids;

    [ThreadStatic]
    private static List<Landform> _tsc_eligible;

    private static void DetermineLandforms(WorldTileInfo info, BiomeProperties biomeProps)
    {
        var eligible = _tsc_eligible;

        List<Landform> landforms = null;

        foreach (var layer in LandformManager.LandformLayers)
        {
            eligible.Clear();

            foreach (var landform in layer.Landforms)
            {
                if (info.CanHaveLandform(landform, biomeProps)) eligible.Add(landform);
            }

            if (layer.LayerId == "")
            {
                foreach (var landform in eligible)
                {
                    if (Rand.ChanceSeeded(landform.WorldTileReq.Commonness, info.MakeSeed(landform.IdHash)))
                    {
                        landforms ??= new(2);
                        landforms.Add(landform);
                    }
                }
            }
            else
            {
                var seed = info.MakeSeed(layer.SelectionSeed);
                var sum = eligible.Sum(lf => lf.WorldTileReq.Commonness);
                var rand = new FloatRange(0f, Math.Max(1f, sum)).RandomInRangeSeeded(seed);

                foreach (var landform in eligible)
                {
                    if (rand < landform.WorldTileReq.Commonness)
                    {
                        landforms ??= new(2);
                        landforms.Add(landform);
                        break;
                    }

                    rand -= landform.WorldTileReq.Commonness;
                }
            }
        }

        if (biomeProps.overrideLandforms != null)
        {
            var ids = _tsc_ids;
            ids.Clear();

            if (landforms != null) ids.AddRange(landforms.Select(lf => lf.Id));

            landforms ??= new(3);
            landforms.Clear();

            foreach (var id in biomeProps.overrideLandforms.Get(new CtxTile(info), ids))
            {
                var landform = LandformManager.FindById(id);
                if (landform != null) landforms.AddDistinct(landform);
            }
        }

        landforms?.Sort((a, b) => a.Priority - b.Priority);
        info.Landforms = landforms;
    }

    public bool CanHaveLandform(Landform landform, BiomeProperties props, bool lenient = false)
    {
        var requirements = landform.WorldTileReq;
        if (requirements == null || !requirements.CheckRequirements(this, lenient)) return false;
        return props.AllowsLandform(landform);
    }

    private static void DetermineBiomeVariants(WorldTileInfoPrimer info)
    {
        List<BiomeVariantDef> variants = null;

        if (info.Biome.Properties().AllowBiomeTransitions)
        {
            foreach (var variant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
            {
                if (variant.worldTileConditions?.Get(new CtxTile(info)) ?? true)
                {
                    variants ??= new(1);
                    variants.Add(variant);
                }
            }
        }

        info.BiomeVariants = variants;
    }

    [ThreadStatic]
    private static List<Rot6> _tsc_waterTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_landTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_cliffTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_nonCliffTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_caveSystemTiles;

    private static void DetermineTopology(WorldTileInfo info, LandformData data, BiomeProperties biomeProps)
    {
        if (biomeProps.isWaterCovered)
        {
            var coastType = WorldTileUtils.CoastTypeFromTile(info.Tile);
            info.Coast = new StructRot4<CoastType>(coastType);
            info.Topology = Topology.Ocean;
            return;
        }

        var grid = info.World.grid;

        int tileId = info.TileId;
        var tileCenter = grid.GetTileCenter(tileId);

        var nbData = grid.tileIDToNeighbors_values;
        var nbOffsets = grid.tileIDToNeighbors_offsets;

        var nbOffset = nbOffsets[tileId];
        var nbBound = nbOffsets.IdxBoundFor(nbData, tileId);

        var waterTiles = _tsc_waterTiles;
        var landTiles = _tsc_landTiles;
        var cliffTiles = _tsc_cliffTiles;
        var nonCliffTiles = _tsc_nonCliffTiles;
        var caveSystemTiles = _tsc_caveSystemTiles;

        waterTiles.Clear();
        landTiles.Clear();
        cliffTiles.Clear();
        nonCliffTiles.Clear();
        caveSystemTiles.Clear();

        List<BorderingBiome> borderingBiomes = null;
        var coast = new StructRot4<CoastType>();

        for (var nbIdx = nbOffset; nbIdx < nbBound; nbIdx++)
        {
            var idx = nbIdx - nbOffset;
            var nbId = nbData[nbIdx];
            var nbTile = grid[nbId];

            // warning: RotateCW and RotateCCW are not reliable and Rot6 indexes may be reversed
            // because tileIDToNeighbors order is cw for some tiles but ccw for others
            var rot6 = new Rot6(idx, grid.GetHeadingFromTo(tileCenter, grid.GetTileCenter(nbId)));

            var coastType = WorldTileUtils.CoastTypeFromTile(nbTile);
            if (coastType != CoastType.None)
            {
                var rot4 = rot6.AsRot4();
                coast[rot4] = WorldTileUtils.CombineCoastTypes(coastType, coast[rot4]);
                nonCliffTiles.Add(rot6);
                waterTiles.Add(rot6);
            }
            else
            {
                if (BiomeTransition.IsTransition(tileId, nbId, info.Biome, nbTile.biome))
                {
                    borderingBiomes ??= new List<BorderingBiome>(2);
                    borderingBiomes.Add(new BorderingBiome(nbTile.biome, rot6.Angle));
                }

                if (info.Tile.hilliness == Hilliness.Impassable)
                {
                    if (nbTile.hilliness < Hilliness.Impassable) nonCliffTiles.Add(rot6);
                    else cliffTiles.Add(rot6);

                    if (data?.GetCaveSystemDepthAt(nbId) > 0) caveSystemTiles.Add(rot6);
                }
                else
                {
                    if ((int) nbTile.hilliness >= 4 && nbTile.hilliness > info.Tile.hilliness) cliffTiles.Add(rot6);
                    else nonCliffTiles.Add(rot6);
                }

                landTiles.Add(rot6);
            }
        }

        if (info.Tile.potentialRivers != null && info.Biome.allowRivers)
        {
            var linkCount = info.Tile.potentialRivers.Count;

            if (linkCount > 1 && info.Tile.potentialRivers.Any(r => grid[r.neighbor].WaterCovered))
            {
                info.River = RiverType.Estuary;
            }
            else
            {
                info.River = linkCount switch
                {
                    0 => RiverType.None,
                    1 => RiverType.Source,
                    2 => RiverType.Normal,
                    _ => RiverType.Confluence
                };
            }
        }

        info.BorderingBiomes = borderingBiomes?.ToArray();
        info.Coast = coast;

        if (nbBound - nbOffset != 6)
        {
            info.Topology = Topology.Inland;
            return;
        }

        if (info.Tile.hilliness == Hilliness.Impassable)
        {
            if (data?.GetCaveSystemDepthAt(tileId) > 0)
            {
                DetermineCaveTopology(info);
                return;
            }

            if (caveSystemTiles.Count > 0)
            {
                info.Topology = Topology.Any;
                return;
            }
        }

        if (waterTiles.Count > 0)
        {
            DetermineCoastTopology(info);
            return;
        }

        if (cliffTiles.Count > 0)
        {
            DetermineCliffTopology(info);
            return;
        }

        info.Topology = Topology.Inland;
        info.TopologyDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));
    }

    private static void DetermineCaveTopology(WorldTileInfo info)
    {
        var nonCliffTiles = _tsc_nonCliffTiles;
        var caveTiles = _tsc_caveSystemTiles;

        if (nonCliffTiles.Count > 0 && caveTiles.Count < 3)
        {
            if (caveTiles.Count > 0)
            {
                var dir = nonCliffTiles.Where(t => caveTiles.Any(t.IsOpposite)).OrderBy(d => d.Slant).FirstOrFallback(Rot6.Invalid);
                if (dir.IsValid)
                {
                    info.Topology = Topology.CaveEntrance;
                    info.TopologyDirection = dir.Opposite.AsRot4();
                    return;
                }
            }

            info.Topology = Topology.CaveEntrance;
            info.TopologyDirection = nonCliffTiles.OrderBy(d => d.Slant).First().Opposite.AsRot4();
            return;
        }

        info.Topology = Topology.CaveTunnel;
        info.TopologyDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));
    }

    private static void DetermineCoastTopology(WorldTileInfo info)
    {
        var waterTiles = _tsc_waterTiles;
        var landTiles = _tsc_landTiles;
        var cliffTiles = _tsc_cliffTiles;

        if (waterTiles.Count == 1)
        {
            info.Topology = cliffTiles.Any(waterTiles[0].IsOpposite) ? Topology.CliffAndCoast : Topology.CoastOneSide;
            info.TopologyDirection = waterTiles[0].AsRot4();
            return;
        }

        if (waterTiles.Count == 2)
        {
            if (waterTiles[0].Adjacent(waterTiles[1]))
            {
                var midPoint = Rot6.MidPoint(waterTiles[0], waterTiles[1]);
                var hasCliff = cliffTiles.Any(o => waterTiles[0].IsOpposite(o) || waterTiles[1].IsOpposite(o));
                info.Topology = hasCliff ? Topology.CliffAndCoast : Topology.CoastOneSide;
                info.TopologyDirection = Rot4.FromAngleFlat(midPoint);
                return;
            }

            if (waterTiles[0].Opposite == waterTiles[1])
            {
                info.Topology = Topology.CoastLandbridge;
                info.TopologyDirection = waterTiles[0].AsRot4().Rotated(Clockwise);
                return;
            }

            var dir = waterTiles[0].Slant < waterTiles[1].Slant ? waterTiles[0] : waterTiles[1];
            info.Topology = cliffTiles.Any(dir.IsOpposite) ? Topology.CliffAndCoast : Topology.CoastOneSide;
            info.TopologyDirection = dir.AsRot4();
            return;
        }

        if (waterTiles.Count == 3)
        {
            var middle = waterTiles.FirstOrFallback(d => waterTiles.All(d.IsSameOrAdjacent), Rot6.Invalid);
            if (middle.IsValid)
            {
                if (middle.Slant > 0.5f)
                {
                    info.Topology = Topology.CoastTwoSides;
                    info.TopologyDirection = Rot4.FromAngleFlat(middle.Angle - 45f);
                    return;
                }

                info.Topology = cliffTiles.Any(middle.IsOpposite) ? Topology.CliffAndCoast : Topology.CoastOneSide;
                info.TopologyDirection = middle.AsRot4();
                return;
            }

            var opp = waterTiles.FirstOrFallback(d => waterTiles.Any(d.IsOpposite), Rot6.Invalid);
            if (opp.IsValid)
            {
                info.Topology = Topology.CoastLandbridge;
                info.TopologyDirection = opp.AsRot4().Rotated(Clockwise);
                return;
            }

            var dir = waterTiles.OrderBy(d => d.Slant).First();
            info.Topology = cliffTiles.Any(dir.IsOpposite) ? Topology.CliffAndCoast : Topology.CoastOneSide;
            info.TopologyDirection = dir.AsRot4();
            return;
        }

        if (waterTiles.Count == 4)
        {
            if (landTiles[0].Adjacent(landTiles[1]))
            {
                info.Topology = Topology.CoastThreeSides;
                info.TopologyDirection = Rot4.FromAngleFlat(Rot6.MidPoint(landTiles[0], landTiles[1]) + 180f);
                return;
            }

            if (landTiles[0] == landTiles[1].Opposite)
            {
                info.Topology = Topology.CoastLandbridge;
                info.TopologyDirection = landTiles[0].AsRot4();
                return;
            }

            var middle = waterTiles.FirstOrFallback(d => waterTiles.Count(d.IsSameOrAdjacent) >= 3, Rot6.Invalid);
            if (middle.IsValid)
            {
                if (middle.Slant > 0.5f)
                {
                    info.Topology = Topology.CoastTwoSides;
                    info.TopologyDirection = Rot4.FromAngleFlat(middle.Angle - 45f);
                    return;
                }

                info.Topology = cliffTiles.Any(middle.IsOpposite) ? Topology.CliffAndCoast : Topology.CoastOneSide;
                info.TopologyDirection = middle.AsRot4();
                return;
            }
        }

        if (waterTiles.Count == 5)
        {
            info.Topology = Topology.CoastThreeSides;
            info.TopologyDirection = landTiles[0].Opposite.AsRot4();
            return;
        }

        if (waterTiles.Count == 6)
        {
            info.Topology = Topology.CoastAllSides;
            info.TopologyDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));
        }
    }

    private static void DetermineCliffTopology(WorldTileInfo info)
    {
        var cliffTiles = _tsc_cliffTiles;
        var nonCliffTiles = _tsc_nonCliffTiles;

        if (cliffTiles.Count == 1)
        {
            info.Topology = Topology.CliffOneSide;
            info.TopologyDirection = cliffTiles[0].AsRot4();
            return;
        }

        if (cliffTiles.Count == 2)
        {
            if (cliffTiles[0].Adjacent(cliffTiles[1]))
            {
                var midPoint = Rot6.MidPoint(cliffTiles[0], cliffTiles[1]);

                if (Rot6.AngleSlant(midPoint) > 0.5f)
                {
                    info.Topology = Topology.CliffTwoSides;
                    info.TopologyDirection = Rot4.FromAngleFlat(midPoint - 45f);
                    return;
                }

                info.Topology = Topology.CliffOneSide;
                info.TopologyDirection = Rot4.FromAngleFlat(midPoint);
                return;
            }

            var dir = cliffTiles[0].Slant < cliffTiles[1].Slant ? cliffTiles[0] : cliffTiles[1];
            info.Topology = Topology.CliffOneSide;
            info.TopologyDirection = dir.AsRot4();
            return;
        }

        if (cliffTiles.Count == 3)
        {
            foreach (var root in cliffTiles)
            {
                var second = cliffTiles.FirstOrFallback(root.IsRotatedCW, Rot6.Invalid);
                if (!second.IsValid) continue;

                var angle = cliffTiles.Any(root.IsRotatedCCW) ? root.Angle :
                    cliffTiles.Any(second.IsRotatedCW) ? second.Angle :
                    Rot6.MidPoint(root, second);

                if (Rot6.AngleSlant(angle) > 0.5f)
                {
                    info.Topology = Topology.CliffTwoSides;
                    info.TopologyDirection = Rot4.FromAngleFlat(angle - 45f);
                    return;
                }

                info.Topology = Topology.CliffOneSide;
                info.TopologyDirection = Rot4.FromAngleFlat(angle);
                return;
            }

            var dir = cliffTiles.OrderBy(d => d.Slant).First();
            info.Topology = Topology.CliffOneSide;
            info.TopologyDirection = dir.AsRot4();
            return;
        }

        if (cliffTiles.Count == 4)
        {
            if (nonCliffTiles[0].Adjacent(nonCliffTiles[1]))
            {
                var midPoint = Rot6.MidPoint(nonCliffTiles[0], nonCliffTiles[1]);

                if (Rot6.AngleSlant(midPoint) > 0.5f)
                {
                    info.Topology = Topology.CliffTwoSides;
                    info.TopologyDirection = Rot4.FromAngleFlat(midPoint + 135f);
                    return;
                }

                info.Topology = Topology.CliffThreeSides;
                info.TopologyDirection = Rot4.FromAngleFlat(midPoint + 180f);
                return;
            }

            var dir = nonCliffTiles[0].Slant < nonCliffTiles[1].Slant ? nonCliffTiles[0] : nonCliffTiles[1];

            if (nonCliffTiles[0].Opposite.IsSameOrAdjacent(nonCliffTiles[1]))
            {
                info.Topology = Topology.CliffValley;
                info.TopologyDirection = dir.AsRot4();
                return;
            }

            info.Topology = Topology.CliffThreeSides;
            info.TopologyDirection = dir.Opposite.AsRot4();
            return;
        }

        if (cliffTiles.Count == 5)
        {
            info.Topology = Topology.CliffThreeSides;
            info.TopologyDirection = nonCliffTiles[0].AsRot4().Opposite;
            return;
        }

        if (cliffTiles.Count == 6)
        {
            info.Topology = Topology.CliffAllSides;
            info.TopologyDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));
        }
    }

    internal float RiverAngle(float baseAngle)
    {
        var topoAngle = TopologyDirection.AsAngle;

        if (Topology is Topology.CoastOneSide or Topology.CliffAndCoast)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle - 30f) > 0f) return topoAngle - 30f;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 30f) < 0f) return topoAngle + 30f;
        }
        else if (Topology == Topology.CoastTwoSides)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle) > 0f) return topoAngle;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 90f) < 0f) return topoAngle + 90f;
        }
        else if (Topology == Topology.CoastThreeSides)
        {
            if (Mathf.DeltaAngle(baseAngle, topoAngle - 15f) > 0f) return topoAngle - 15f;
            if (Mathf.DeltaAngle(baseAngle, topoAngle + 15f) < 0f) return topoAngle + 15f;
        }

        return baseAngle;
    }

    internal Vector3 RiverPosition(int salt)
    {
        var x = Rand.RangeSeeded(0.3f, 0.7f, MakeSeed(9332 + salt));
        var z = Rand.RangeSeeded(0.3f, 0.7f, MakeSeed(2750 + salt));
        return new Vector3(x, 0f, z);
    }

    public override string ToString() =>
        $"{nameof(TileId)}: {TileId}, " +
        $"{nameof(Landforms)}: {Landforms?.Join(null, " ")}, " +
        $"{nameof(BiomeVariants)}: {BiomeVariants?.Join(null, " ")}, " +
        $"{nameof(Topology)}: {Topology}, " +
        $"{nameof(TopologyValue)}: {TopologyValue}, " +
        $"{nameof(TopologyDirection)}: {TopologyDirection.ToStringWord()}";
}

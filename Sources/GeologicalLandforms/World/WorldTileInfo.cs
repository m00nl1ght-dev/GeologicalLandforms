using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static GeologicalLandforms.LandformData;
using static Verse.RotationDirection;

#if RW_1_6_OR_GREATER
using static RimWorld.Planet.SurfaceTile;
#else
using static RimWorld.Planet.Tile;
#endif

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery

namespace GeologicalLandforms;

public class WorldTileInfo : IWorldTileInfo
{
    public IReadOnlyList<Landform> Landforms { get; protected set; }
    public IReadOnlyList<BorderingBiome> BorderingBiomes { get; protected set; }
    public IReadOnlyList<BiomeVariantDef> BiomeVariants { get; protected set; }

    public Topology Topology { get; protected set; }
    public float TopologyValue { get; protected set; }
    public Rot4 TopologyDirection { get; protected set; }
    public StructRot4<CoastType> Coast { get; protected set; }
    public RiverType RiverType { get; protected set; }

    public MapParent WorldObject => World.worldObjects?.MapParentAt(TileId);

    public BiomeDef Biome => Tile.biome;
    public Hilliness Hilliness => Tile.hilliness;
    public float Elevation => Tile.elevation;
    public float Temperature => Tile.temperature;
    public float Rainfall => Tile.rainfall;
    public float Swampiness => Tile.swampiness;
    public float Pollution => Tile.pollution;
    public bool HasCaves => World.HasCaves(TileId);

    public RiverDef MainRiver => Tile.LargestRiverLink().river;
    public RoadDef MainRoad => Tile.LargestRoadLink().road;

    public IRiverData Rivers => GetOrCreateTileLinkData();
    public IRoadData Roads => GetOrCreateTileLinkData();

    #if RW_1_6_OR_GREATER
    public Landmark Landmark => Tile.Landmark;
    #endif

    public Vector3 PosInWorld => World.grid.GetTileCenter(TileId);

    public byte DepthInCaveSystem => World.LandformData()?.GetCaveSystemDepthAt(TileId) ?? 0;

    public int StableSeed(int salt) => WorldTileUtils.StableSeedForTile(TileId, salt);

    internal readonly int TileId;

    #if RW_1_6_OR_GREATER
    internal readonly SurfaceTile Tile;
    #else
    internal readonly Tile Tile;
    #endif

    internal readonly World World;

    private TileLinkData _tileLinkData;
    private int _cacheVersion;

    #if RW_1_6_OR_GREATER
    protected WorldTileInfo(int tileId, SurfaceTile tile, World world)
    #else
    protected WorldTileInfo(int tileId, Tile tile, World world)
    #endif
    {
        TileId = tileId;
        Tile = tile;
        World = world;
    }

    private static WorldTileInfoPrimer[] _cache;
    private static int _validCacheVersion = 1;

    public static IWorldTileInfo Get(Map map, bool allowFromCache = true)
    {
        #if RW_1_5_OR_GREATER

        // Support for vanilla pocket maps introduced in 1.5
        if (map.IsPocketMap)
        {
            var properties = map.generatorDef?.GetModExtension<PocketMapProperties>();
            return PocketMapInfo.Get(map.pocketTileInfo ?? new Tile { biome = map.Biome }, properties);
        }

        #endif

        // Support for special-purpose maps from mods that patch the map.Biome getter (e.g. DeepRim, SOS2)
        if (map.Biome != map.TileInfo.biome)
        {
            return PocketMapInfo.Get(new Tile { biome = map.Biome });
        }

        return Get(map.Tile, allowFromCache);
    }

    public static IWorldTileInfo Get(int tileId, bool allowFromCache = true)
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

            var worldData = world.LandformData();
            var tileData = worldData != null && worldData.TryGet(tileId, out var found) ? found : null;

            if (_tsc_waterTiles == null)
            {
                _tsc_waterTiles = new(6);
                _tsc_landTiles = new(6);
                _tsc_cliffTiles = new(6);
                _tsc_nonCliffTiles = new(6);
                _tsc_caveSystemTiles = new(6);
                _tsc_eligible = new(20);
                _tsc_commonness = new(20);
                _tsc_ids = new(5);
            }

            DetermineTopology(info, worldData, props);

            tileData?.ApplyTopology(info);

            DetermineLandforms(info, tileData, props);
            DetermineBiomeVariants(info, tileData, props);

            GeologicalLandformsAPI.WorldTileInfoHook.Apply(world, info);

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

            #if RW_1_6_OR_GREATER
            return new WorldTileInfo(tileId, new SurfaceTile { biome = BiomeDefOf.Ocean, elevation = 0f }, Find.World);
            #else
            return new WorldTileInfo(tileId, new Tile { biome = BiomeDefOf.Ocean, elevation = 0f }, Find.World);
            #endif
        }
    }

    public static void InvalidateCache()
    {
        unchecked { _validCacheVersion++; }

        #if RW_1_6_OR_GREATER
        TileMutatorsCustomization.ClearCache();
        #endif
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

    [ThreadStatic]
    private static List<float> _tsc_commonness;

    private static void DetermineLandforms(WorldTileInfo info, TileData tileData, BiomeProperties biomeProps)
    {
        var eligible = _tsc_eligible;
        var commonness = _tsc_commonness;

        List<Landform> landforms = null;

        if (tileData != null)
        {
            if (tileData.Landforms != null)
            {
                foreach (var landformId in tileData.Landforms)
                {
                    var landform = LandformManager.FindById(landformId);

                    if (landform != null)
                    {
                        landforms ??= new(2);
                        landforms.Add(landform);
                    }
                    else
                    {
                        GeologicalLandformsAPI.Logger.Warn($"No landform found with id {landformId}");
                    }
                }
            }
        }
        else
        {
            foreach (var layer in LandformManager.LandformLayers)
            {
                eligible.Clear();
                commonness.Clear();

                foreach (var landform in layer.Landforms)
                {
                    var value = landform.GetCommonnessForTile(info);
                    if (value > 0f && biomeProps.AllowsLandform(landform))
                    {
                        eligible.Add(landform);
                        commonness.Add(value);
                    }
                }

                if (layer.LayerId == "")
                {
                    for (var i = 0; i < eligible.Count; i++)
                    {
                        if (RandAsync.Chance(commonness[i], info.StableSeed(eligible[i].IdHash)))
                        {
                            landforms ??= new(2);
                            landforms.Add(eligible[i]);
                        }
                    }
                }
                else
                {
                    var seed = info.StableSeed(layer.SelectionSeed);
                    var rand = RandAsync.Range(0f, Math.Max(1f, commonness.Sum()), seed);

                    for (var i = 0; i < eligible.Count; i++)
                    {
                        if (rand < commonness[i])
                        {
                            landforms ??= new(2);
                            landforms.Add(eligible[i]);
                            break;
                        }

                        rand -= commonness[i];
                    }
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

        if (landforms != null)
        {
            landforms.RemoveAll(lf => !GeologicalLandformsAPI.LandformEnabled.Apply(lf));

            foreach (var nodeApplyLayer in landforms.SelectMany(lf => lf.ApplyLayers).ToList())
            {
                var landform = LandformManager.FindById(nodeApplyLayer.LayerId);
                if (landform != null) landforms.AddDistinct(landform);
            }

            landforms.Sort((a, b) => a.Priority - b.Priority);
        }

        info.Landforms = landforms;
    }

    private static void DetermineBiomeVariants(WorldTileInfoPrimer info, TileData tileData, BiomeProperties biomeProps)
    {
        List<BiomeVariantDef> variants = null;

        var ctxTile = new CtxTile(info);

        if (tileData != null)
        {
            if (tileData.BiomeVariants != null)
            {
                foreach (var defName in tileData.BiomeVariants)
                {
                    var def = DefDatabase<BiomeVariantDef>.GetNamed(defName);

                    if (def != null)
                    {
                        variants ??= new(2);
                        variants.Add(def);
                    }
                    else
                    {
                        GeologicalLandformsAPI.Logger.Warn($"No biome variant found with def name {defName}");
                    }
                }
            }
        }
        else if (biomeProps.AllowBiomeTransitions)
        {
            foreach (var variant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
            {
                if (variant.worldTileConditions?.Get(ctxTile) ?? true)
                {
                    variants ??= new(1);
                    variants.Add(variant);
                }
            }
        }

        if (biomeProps.overrideBiomeVariants != null)
        {
            var ids = _tsc_ids;
            ids.Clear();

            if (variants != null) ids.AddRange(variants.Select(bv => bv.defName));

            variants ??= new(3);
            variants.Clear();

            foreach (var id in biomeProps.overrideBiomeVariants.Get(ctxTile, ids))
            {
                var variant = DefDatabase<BiomeVariantDef>.GetNamed(id, false);
                if (variant != null) variants.AddDistinct(variant);
            }
        }

        variants?.RemoveAll(bv => !GeologicalLandformsAPI.BiomeVariantEnabled.Apply(bv));

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

        var nbData = grid.ExtNbValues();
        var nbOffsets = grid.ExtNbOffsets();

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

        var nbRiverCount = 0;

        for (var nbIdx = nbOffset; nbIdx < nbBound; nbIdx++)
        {
            var idx = nbIdx - nbOffset;
            var nbId = (int) nbData[nbIdx];
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

                if (nbTile.Rivers is { Count: > 0 }) nbRiverCount++;

                landTiles.Add(rot6);
            }
        }

        if (info.Tile.potentialRivers != null && info.Biome.allowRivers)
        {
            info.RiverType = info.Tile.potentialRivers.Count switch
            {
                0 => RiverType.None,
                1 => RiverType.Source,
                2 => RiverType.Normal,
                _ => info.Tile.potentialRivers.Count(r => !grid[r.neighbor].WaterCovered) > 1
                    ? RiverType.Confluence
                    : RiverType.Estuary
            };
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

        if (info.Tile.hilliness == Hilliness.Impassable && (caveSystemTiles.Count > 0 || nbRiverCount > 0))
        {
            info.Topology = Topology.Any;
            return;
        }

        if (cliffTiles.Count > 0)
        {
            DetermineCliffTopology(info);
            return;
        }

        info.Topology = Topology.Inland;
        info.TopologyDirection = new Rot4(RandAsync.Range(0, 4, info.StableSeed(0087)));
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
        info.TopologyDirection = new Rot4(RandAsync.Range(0, 4, info.StableSeed(0087)));
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
            info.TopologyDirection = new Rot4(RandAsync.Range(0, 4, info.StableSeed(0087)));
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
            info.TopologyDirection = new Rot4(RandAsync.Range(0, 4, info.StableSeed(0087)));
        }
    }

    private class TileLinkData : IRiverData, IRoadData
    {
        public float RiverInflowAngle { get; internal set; }
        public float RiverInflowOffset { get; internal set; }
        public float RiverInflowWidth { get; internal set; }
        public float RiverTributaryAngle { get; internal set; }
        public float RiverTributaryOffset { get; internal set; }
        public float RiverTributaryWidth { get; internal set; }
        public float RiverTertiaryWidth { get; internal set; }
        public float RiverTertiaryAngle { get; internal set; }
        public float RiverTertiaryOffset { get; internal set; }
        public float RiverOutflowAngle { get; internal set; }
        public float RiverOutflowWidth { get; internal set; }

        public float RoadPrimaryAngle { get; internal set; }
        public float RoadSecondaryAngle { get; internal set; }
    }

    private TileLinkData GetOrCreateTileLinkData()
    {
        if (_tileLinkData == null)
        {
            var data = new TileLinkData();

            var riverLinks = Tile.Rivers;

            if (riverLinks != null)
            {
                RiverLink inflow = default;
                RiverLink tributary = default;
                RiverLink tertiary = default;
                RiverLink outflow = default;

                foreach (var link in riverLinks)
                {
                    if (WorldTileUtils.IsRiverInflow(World.grid, TileId, link))
                    {
                        if (link.river.WidthOnWorld() > inflow.river.WidthOnWorld())
                        {
                            tertiary = tributary;
                            tributary = inflow;
                            inflow = link;
                        }
                        else if (link.river.WidthOnWorld() > tributary.river.WidthOnWorld())
                        {
                            tertiary = tributary;
                            tributary = link;
                        }
                        else if (link.river.WidthOnWorld() > tertiary.river.WidthOnWorld())
                        {
                            tertiary = link;
                        }
                    }
                    else
                    {
                        if (link.river.WidthOnWorld() > outflow.river.WidthOnWorld()) outflow = link;
                    }
                }

                if (inflow.river != null)
                {
                    var position = WorldTileUtils.RiverPositionForTile(this, 0);
                    data.RiverInflowAngle = World.grid.GetHeadingFromTo(inflow.neighbor, TileId);
                    data.RiverInflowOffset = WorldTileUtils.RiverPositionToOffset(position, data.RiverInflowAngle);
                    data.RiverInflowWidth = inflow.river.widthOnMap;
                }

                if (tributary.river != null)
                {
                    var position = WorldTileUtils.RiverPositionForTile(this, 1);
                    data.RiverTributaryAngle = World.grid.GetHeadingFromTo(tributary.neighbor, TileId);
                    data.RiverTributaryOffset = WorldTileUtils.RiverPositionToOffset(position, data.RiverTributaryAngle);
                    data.RiverTributaryWidth = tributary.river.widthOnMap;
                }

                if (tertiary.river != null)
                {
                    var position = WorldTileUtils.RiverPositionForTile(this, 2);
                    data.RiverTertiaryAngle = World.grid.GetHeadingFromTo(tertiary.neighbor, TileId);
                    data.RiverTertiaryOffset = WorldTileUtils.RiverPositionToOffset(position, data.RiverTertiaryAngle);
                    data.RiverTertiaryWidth = tertiary.river.widthOnMap;
                }

                if (outflow.river != null)
                {
                    var baseAngle = World.grid.GetHeadingFromTo(TileId, outflow.neighbor);
                    data.RiverOutflowAngle = WorldTileUtils.RiverAngleForTile(this, baseAngle);
                    data.RiverOutflowWidth = outflow.river.widthOnMap;
                }
            }

            var roadLinks = Tile.Roads;

            if (roadLinks != null)
            {
                RoadLink primary = default;
                RoadLink secondary = default;

                foreach (var link in roadLinks)
                {
                    if (link.road.WidthOnWorld() > primary.road.WidthOnWorld())
                    {
                        secondary = primary;
                        primary = link;
                    }
                    else if (link.road.WidthOnWorld() > secondary.road.WidthOnWorld())
                    {
                        secondary = link;
                    }
                }

                if (primary.road != null)
                {
                    data.RoadPrimaryAngle = World.grid.GetHeadingFromTo(primary.neighbor, TileId);
                }

                if (secondary.road != null)
                {
                    data.RoadSecondaryAngle = World.grid.GetHeadingFromTo(secondary.neighbor, TileId);
                }
            }

            _tileLinkData = data;
        }

        return _tileLinkData;
    }

    public override string ToString() =>
        $"{nameof(TileId)}: {TileId}, " +
        $"{nameof(Landforms)}: {Landforms?.Join(null, " & ") ?? "None"}, " +
        $"{nameof(BiomeVariants)}: {BiomeVariants?.Join(null, " & ") ?? "None"}, " +
        $"{nameof(Topology)}: {Topology}, " +
        $"{nameof(TopologyDirection)}: {TopologyDirection.ToStringWord()}";
}

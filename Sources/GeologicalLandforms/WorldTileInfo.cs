using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;
using static Verse.RotationDirection;
using static GeologicalLandforms.IWorldTileInfo;

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

    public Topology Topology { get; protected set; } = Topology.Any;
    public Rot4 LandformDirection { get; protected set; }

    public StructRot4<CoastType> Coast { get; protected set; }

    public MapParent WorldObject => World.worldObjects.MapParentAt(TileId);
    public BiomeDef Biome => Tile.biome;

    public Hilliness Hilliness => Tile.hilliness;
    public float Elevation => Tile.elevation;
    public float Temperature => Tile.temperature;
    public float Rainfall => Tile.rainfall;
    public float Swampiness => Tile.swampiness;

    public Tile.RiverLink? MainRiverLink => Tile.Rivers?.OrderBy(r => -r.river.degradeThreshold).FirstOrDefault();
    public Tile.RoadLink? MainRoadLink => Tile.Roads?.OrderBy(r => r.road.movementCostMultiplier).FirstOrDefault();

    public RiverDef MainRiver => MainRiverLink?.river;
    public float MainRiverAngle => World.grid.GetHeadingFromTo(TileId, MainRiverLink?.neighbor ?? 0);

    public RoadDef MainRoad => MainRoadLink?.road;
    public float MainRoadAngle => World.grid.GetHeadingFromTo(TileId, MainRoadLink?.neighbor ?? 0);

    public int MakeSeed(int hash) => Patch_RimWorld_World.LastKnownInitialWorldSeed ^ TileId ^ hash; // TODO use proper hash

    protected WorldTileInfo(int tileId, Tile tile, World world)
    {
        TileId = tileId;
        Tile = tile;
        World = world;
    }

    private static WorldTileInfoPrimer[] _cache;
    private static int _validCacheVersion = 1;

    private int _cacheVersion;

    public static WorldTileInfo Get(int tileId)
    {
        try
        {
            var cache = _cache;
            var validVersion = _validCacheVersion;

            var world = Find.World;
            var canUseCache = cache != null && tileId < cache.Length;

            if (canUseCache)
            {
                var match = cache[tileId];
                if (match != null && match._cacheVersion == validVersion && match.World == world) return match;
            }

            var info = new WorldTileInfoPrimer(tileId, world.grid[tileId], world);

            DetermineTopology(info);
            DetermineLandforms(info);
            DetermineBiomeVariants(info);

            GeologicalLandformsAPI.ApplyWorldTileInfoHook(info);

            if (canUseCache)
            {
                info._cacheVersion = validVersion;
                cache[tileId] = info;
            }

            return info;
        }
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
    private static List<Landform> _tsc_eligible;

    private static void DetermineLandforms(WorldTileInfo info)
    {
        var eligible = _tsc_eligible ??= new List<Landform>();
        eligible.Clear();

        eligible.AddRange(LandformManager.Landforms.Values
            .Where(e => info.CanHaveLandform(e))
            .OrderBy(e => e.Manifest.TimeCreated));

        var landforms = eligible.Where(e => e.IsLayer && Rand.ChanceSeeded(e.WorldTileReq.Commonness, info.MakeSeed(e.RandSeed)));

        var landformData = info.World.LandformData();
        if (landformData != null && landformData.TryGet(info.TileId, out var data))
        {
            var main = data.Landform;
            if (main != null) landforms = landforms.Append(main);
            info.LandformDirection = data.LandformDirection;
        }
        else
        {
            var eligibleForMain = eligible.Where(e => !e.IsLayer);

            float sum = Math.Max(1f, eligibleForMain.Sum(e => e.WorldTileReq.Commonness));
            float rand = new FloatRange(0f, sum).RandomInRangeSeeded(info.MakeSeed(1754));

            Landform main = null;
            foreach (var landform in eligibleForMain)
            {
                if (rand < landform.WorldTileReq.Commonness)
                {
                    main = landform;
                    break;
                }

                rand -= landform.WorldTileReq.Commonness;
            }

            if (main != null) landforms = landforms.Append(main);
        }

        info.Landforms = landforms.OrderBy(e => e.Priority).ToList();
    }

    public bool CanHaveLandform(Landform landform, bool lenient = false)
    {
        if (Hilliness == Hilliness.Impassable) return false;
        var requirements = landform.WorldTileReq;
        if (requirements == null || !requirements.CheckRequirements(this, lenient)) return false;
        var biomeProperties = Biome.Properties();
        if (!biomeProperties.AllowLandforms && (!landform.IsLayer || !biomeProperties.AllowBiomeTransitions)) return false;
        if (biomeProperties.disallowedLandforms?.Contains(landform.Id) ?? false) return false;
        return true;
    }

    private static void DetermineBiomeVariants(WorldTileInfoPrimer info)
    {
        List<BiomeVariantDef> variants = null;

        if (info.Biome.Properties().AllowBiomeTransitions)
        {
            foreach (var variant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
            {
                if (variant.worldTileConditions.Evaluate(info))
                {
                    variants ??= new();
                    variants.Add(variant);
                }
            }
        }

        info.BiomeVariants = variants;
    }

    [ThreadStatic]
    private static List<int> _tsc_nbIds;

    [ThreadStatic]
    private static List<Rot6> _tsc_waterTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_landTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_cliffTiles;

    [ThreadStatic]
    private static List<Rot6> _tsc_nonCliffTiles;

    private static void DetermineTopology(WorldTileInfo info)
    {
        var selfBiome = info.Biome;
        var grid = info.World.grid;
        int tileId = info.TileId;

        info.LandformDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));

        if (selfBiome.Properties().isWaterCovered)
        {
            info.Coast = new StructRot4<CoastType>(CoastTypeFromTile(info.Tile));
            info.Topology = Topology.Ocean;
            return;
        }

        var nb = _tsc_nbIds ??= new List<int>();
        grid.GetTileNeighbors(info.TileId, nb);
        nb.SortBy(n => (grid.GetHeadingFromTo(tileId, n) + 30f) % 360f);

        var waterTiles = _tsc_waterTiles ??= new List<Rot6>();
        var landTiles = _tsc_landTiles ??= new List<Rot6>();
        var cliffTiles = _tsc_cliffTiles ??= new List<Rot6>();
        var nonCliffTiles = _tsc_nonCliffTiles ??= new List<Rot6>();

        waterTiles.Clear();
        landTiles.Clear();
        cliffTiles.Clear();
        nonCliffTiles.Clear();

        List<BorderingBiome> borderingBiomes = null;
        var coast = new StructRot4<CoastType>();

        for (var i = 0; i < nb.Count; i++)
        {
            var nbId = nb[i];
            var nbTile = grid[nbId];
            var rot6 = new Rot6(i, grid.GetHeadingFromTo(tileId, nbId));
            var rot4 = rot6.AsRot4();

            var coastType = CoastTypeFromTile(nbTile);
            if (coastType != CoastType.None)
            {
                waterTiles.Add(rot6);
                nonCliffTiles.Add(rot6);
                coast[rot4] = CombineCoastTypes(coastType, coast[rot4]);
            }
            else
            {
                if (BiomeTransition.IsTransition(tileId, nbId, selfBiome, nbTile.biome))
                {
                    borderingBiomes ??= new List<BorderingBiome>();
                    borderingBiomes.Add(new BorderingBiome(nbTile.biome, rot6.Angle));
                }

                if ((int) nbTile.hilliness >= 4 && nbTile.hilliness > info.Tile.hilliness) cliffTiles.Add(rot6);
                else nonCliffTiles.Add(rot6);
                landTiles.Add(rot6);
            }
        }

        info.BorderingBiomes = borderingBiomes;
        info.Coast = coast;

        if (nb.Count != 6)
        {
            info.Topology = Topology.Inland;
            return;
        }

        if (waterTiles.Count == 0)
        {
            info.Topology = Topology.Inland;
            DetermineCliffTopology(info, Rot6.Invalid, cliffTiles, nonCliffTiles);
            return;
        }

        if (waterTiles.Count == 1)
        {
            info.LandformDirection = waterTiles[0].AsRot4();
            info.Topology = Topology.CoastOneSide;
            DetermineCliffTopology(info, waterTiles[0], cliffTiles, nonCliffTiles);
            return;
        }

        if (waterTiles.Count == 2)
        {
            if (waterTiles[0].Adjacent(waterTiles[1]))
            {
                var midPoint = Rot6.MidPoint(waterTiles[0], waterTiles[1]);
                info.LandformDirection = Rot4.FromAngleFlat(midPoint);
                info.Topology = Topology.CoastOneSide;
                DetermineCliffTopology(info, waterTiles[0], cliffTiles, nonCliffTiles);
                return;
            }

            if (waterTiles[0].Opposite == waterTiles[1])
            {
                info.LandformDirection = waterTiles[0].AsRot4().Rotated(Clockwise);
                info.Topology = Topology.CoastLandbridge;
                return;
            }

            var random = waterTiles.RandomElementSeeded(info.MakeSeed(1785));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CoastOneSide;
            return;
        }

        if (waterTiles.Count == 3)
        {
            var middle = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.RotatedCW()) && waterTiles.Contains(d.RotatedCCW()), Rot6.Invalid);
            if (middle.IsValid)
            {
                DetermineCoastTopology(info, middle, 0.25f);
                return;
            }

            var opp = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.Opposite), Rot6.Invalid);
            if (opp.IsValid)
            {
                info.LandformDirection = opp.AsRot4().Rotated(Clockwise);
                info.Topology = Topology.CoastLandbridge;
                return;
            }

            info.Topology = Topology.Inland;
            return;
        }

        if (waterTiles.Count == 4)
        {
            if (landTiles[0] == landTiles[1].RotatedCW())
            {
                DetermineCoastTopology(info, landTiles[1].Opposite, 0.5f, true);
                return;
            }

            if (landTiles[0] == landTiles[1].RotatedCCW())
            {
                DetermineCoastTopology(info, landTiles[0].Opposite, 0.5f, true);
                return;
            }

            if (landTiles[0] == landTiles[1].Opposite)
            {
                info.LandformDirection = landTiles[0].AsRot4();
                info.Topology = Topology.CoastLandbridge;
                return;
            }

            var middle = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.RotatedCW()) && waterTiles.Contains(d.RotatedCCW()), Rot6.Invalid);
            if (middle.IsValid)
            {
                DetermineCoastTopology(info, middle, 0.25f);
                return;
            }

            info.Topology = Topology.Any;
            return;
        }

        if (waterTiles.Count == 5)
        {
            DetermineCoastTopology(info, landTiles[0].Opposite, 1f);
            return;
        }

        if (waterTiles.Count == 6)
        {
            info.Topology = Topology.CoastAllSides;
            return;
        }

        info.Topology = Topology.Any;
    }

    public static void DetermineCoastTopology(WorldTileInfo info, Rot6 dir, float chance, bool exact = false)
    {
        if (Rand.ChanceSeeded(chance, info.MakeSeed(8021)))
        {
            info.LandformDirection = dir.AsRot4();
            info.Topology = Topology.CoastThreeSides;
            return;
        }

        if (dir.RotatedCW().AsRot4() == dir.AsRot4())
        {
            info.LandformDirection = dir.RotatedCCW().AsRot4();
            info.Topology = Topology.CoastTwoSides;
            return;
        }

        info.LandformDirection = exact || Rand.ChanceSeeded(0.5f, info.MakeSeed(9777)) ? dir.AsRot4() : dir.RotatedCCW().AsRot4();
        info.Topology = Topology.CoastTwoSides;
    }

    public static void DetermineCliffTopology(WorldTileInfo info, Rot6 coastDir, List<Rot6> cliffTiles, List<Rot6> nonCliffTiles)
    {
        if (cliffTiles.Count == 0)
        {
            return;
        }

        if (coastDir.IsValid)
        {
            if (cliffTiles.Contains(coastDir.Opposite))
            {
                info.Topology = Topology.CliffAndCoast;
            }

            return;
        }

        if (cliffTiles.Count == 1)
        {
            info.LandformDirection = cliffTiles[0].AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }

        if (cliffTiles.Count == 2)
        {
            var dir0 = cliffTiles[0].AsRot4();
            var dir1 = cliffTiles[1].AsRot4();

            if (dir0 == dir1)
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CliffOneSide;
                return;
            }

            if (cliffTiles[1] == cliffTiles[0].Rotated(Clockwise))
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CliffTwoSides;
                return;
            }

            if (cliffTiles[1] == cliffTiles[0].Rotated(Counterclockwise))
            {
                info.LandformDirection = dir1;
                info.Topology = Topology.CliffTwoSides;
                return;
            }

            var random = cliffTiles.RandomElementSeeded(info.MakeSeed(2573));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }

        if (cliffTiles.Count == 3)
        {
            var first = cliffTiles.Where(d => cliffTiles.Contains(d.RotatedCW()) && d.AsRot4() != d.RotatedCW().AsRot4())
                .ToList().RandomElementSeeded(info.MakeSeed(2647), Rot6.Invalid);

            if (first.IsValid)
            {
                info.LandformDirection = first.AsRot4();
                info.Topology = Topology.CliffTwoSides;
                return;
            }

            var random = cliffTiles.RandomElementSeeded(info.MakeSeed(1142));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }

        if (cliffTiles.Count == 4 && nonCliffTiles.Count == 2)
        {
            if (nonCliffTiles[0] == nonCliffTiles[1].RotatedCW() || nonCliffTiles[0] == nonCliffTiles[1].RotatedCCW())
            {
                info.LandformDirection = nonCliffTiles.RandomElementSeeded(info.MakeSeed(1455)).AsRot4().Opposite;
                info.Topology = Topology.CliffThreeSides;
                return;
            }

            if (nonCliffTiles[0] == nonCliffTiles[1].Opposite)
            {
                info.LandformDirection = nonCliffTiles[0].AsRot4();
                info.Topology = Topology.CliffValley;
                return;
            }

            var first = cliffTiles.Where(d => cliffTiles.Contains(d.RotatedCW()) && d.AsRot4() != d.RotatedCW().AsRot4())
                .ToList().RandomElementSeeded(info.MakeSeed(1675), Rot6.Invalid);

            if (first.IsValid)
            {
                info.LandformDirection = first.AsRot4();
                info.Topology = Topology.CliffTwoSides;
                return;
            }

            var random = cliffTiles.RandomElementSeeded(info.MakeSeed(1675));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }

        if (cliffTiles.Count == 5 && nonCliffTiles.Count == 1)
        {
            info.LandformDirection = nonCliffTiles[0].AsRot4().Opposite;
            info.Topology = Topology.CliffThreeSides;
            return;
        }

        if (cliffTiles.Count == 6)
        {
            info.Topology = Topology.CliffAllSides;
        }
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

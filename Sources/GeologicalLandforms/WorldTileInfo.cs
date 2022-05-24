using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class WorldTileInfo : IWorldTileInfo
{
    public readonly int TileId;
    public readonly Tile Tile;
    public readonly World World;

    public IReadOnlyList<Landform> Landforms => _landforms;
    private List<Landform> _landforms;
    
    public IReadOnlyList<IWorldTileInfo.BorderingBiome> BorderingBiomes => _borderingBiomes;
    private List<IWorldTileInfo.BorderingBiome> _borderingBiomes;

    public Topology Topology { get; private set; } = Topology.Any;
    public Rot4 LandformDirection { get; private set; }
    
    public MapParent WorldObject => World.worldObjects.MapParentAt(TileId);
    public BiomeDef Biome => Tile.biome;

    public Hilliness Hilliness => Tile.hilliness;
    public float Elevation => Tile.elevation;
    public float Temperature => Tile.temperature;
    public float Rainfall => Tile.rainfall;
    public float Swampiness => Tile.swampiness;
    
    public bool HasOcean { get; private set; }

    public RiverDef MainRiver { get; private set; }
    public float MainRiverAngle { get; private set; }
    
    public RoadDef MainRoad { get; private set; }
    public float MainRoadAngle { get; private set; }

    public int MakeSeed(int hash) => RimWorld_World.LastKnownInitialWorldSeed ^ TileId ^ hash;

    public WorldTileInfo(int tileId, Tile tile, World world)
    {
        TileId = tileId;
        Tile = tile;
        World = world;
    }

    private static WorldTileInfo _cache;
    
    public static WorldTileInfo Get(int tileId)
    {
        World world = Find.World;
        if (_cache != null && _cache.TileId == tileId && _cache.World == world) return _cache;
        _cache = new WorldTileInfo(tileId, world.grid[tileId], world);
        DetermineTopology(_cache);
        DetermineLandforms(_cache);
        return _cache;
    }

    public static void InvalidateCache()
    {
        _cache = null;
    }

    private static void DetermineLandforms(WorldTileInfo info)
    {
        if (!info.Biome.canBuildBase || info.Hilliness == Hilliness.Impassable) return; 
        if (Main.IsBiomeExcluded(info.Biome)) return;

        var eligible = LandformManager.Landforms.Values
            .Where(e => e.WorldTileReq?.CheckRequirements(info) ?? false)
            .OrderBy(e => e.Manifest.TimeCreated)
            .ToList();
        
        var eligibleForMain = eligible.Where(e => !e.IsLayer).ToList();

        float sum = Math.Max(1f, eligibleForMain.Sum(e => e.WorldTileReq.Commonness));
        float rand = new FloatRange(0f, sum).RandomInRangeSeeded(info.MakeSeed(1754));

        Landform main = null;
        foreach (Landform landform in eligibleForMain)
        {
            if (rand < landform.WorldTileReq.Commonness)
            {
                main = landform;
                break;
            }

            rand -= landform.WorldTileReq.Commonness;
        }

        var landforms = eligible.Where(e => e.IsLayer && Rand.ChanceSeeded(e.WorldTileReq.Commonness, info.MakeSeed(e.RandSeed)));
        if (main != null) landforms = landforms.Append(main);
        info._landforms = landforms.OrderByDescending(e => e.Priority).ToList();
    }

    private static void DetermineTopology(WorldTileInfo info)
    {
        BiomeDef biome = info.Biome;
        WorldGrid grid = info.World.grid;
        int tileId = info.TileId;

        info.LandformDirection = new Rot4(Rand.RangeInclusiveSeeded(0, 3, info.MakeSeed(0087)));

        if (biome == BiomeDefOf.Lake || biome == BiomeDefOf.Ocean && (info.HasOcean = true))
        {
            info.Topology = Topology.Ocean;
            return;
        }
        
        var rivers = info.Tile.Rivers;
        if (rivers?.Count > 0)
        {
            Tile.RiverLink riverLink = rivers.OrderBy(r => -r.river.degradeThreshold).First();
            info.MainRiverAngle = info.World.grid.GetHeadingFromTo(info.TileId, riverLink.neighbor);
            info.MainRiver = riverLink.river;
        }
        
        var roads = info.Tile.Roads;
        if (roads?.Count > 0)
        {
            Tile.RoadLink roadLink = roads.OrderBy(r => r.road.movementCostMultiplier).First();
            info.MainRoadAngle = info.World.grid.GetHeadingFromTo(info.TileId, roadLink.neighbor);
            info.MainRoad = roadLink.road;
        }

        if (Main.IsBiomeExcluded(biome))
        {
            info.Topology = Topology.Any;
            return;
        }

        List<int> nb = new();
        grid.GetTileNeighbors(info.TileId, nb);

        List<Rot6> waterTiles = new();
        List<Rot6> cliffTiles = new();
        foreach (var nbTileId in nb)
        {
            Tile nbTile = grid[nbTileId];
            
            if (nbTile.biome == BiomeDefOf.Lake || nbTile.biome == BiomeDefOf.Ocean && (info.HasOcean = true))
            {
                Rot6 rotFromTo = Rot6.FromAngleFlat(grid.GetHeadingFromTo(tileId, nbTileId));
                if (!waterTiles.Contains(rotFromTo)) waterTiles.Add(rotFromTo);
            }
            else
            {
                Rot6 rotFromTo = Rot6.FromAngleFlat(grid.GetHeadingFromTo(tileId, nbTileId));
                if (nbTile.biome != biome && nbTile.biome.canBuildBase && !Main.IsBiomeExcluded(nbTile.biome))
                {
                    info._borderingBiomes ??= new List<IWorldTileInfo.BorderingBiome>();
                    info._borderingBiomes.Add(new IWorldTileInfo.BorderingBiome(nbTile.biome, rotFromTo.AsAngle));
                }
                
                if (((int) nbTile.hilliness) >= 3 && nbTile.hilliness - info.Tile.hilliness > 0)
                {
                    if (!cliffTiles.Contains(rotFromTo)) cliffTiles.Add(rotFromTo);
                }
            }
        }

        if (waterTiles.Count == 0)
        {
            info.Topology = Topology.Inland;
            DetermineCliffTopology(info, Rot6.Invalid, cliffTiles);
            return;
        }
        
        if (waterTiles.Count == 1)
        {
            info.LandformDirection = waterTiles[0].AsRot4();
            info.Topology = Topology.CoastOneSide;
            DetermineCliffTopology(info, waterTiles[0], cliffTiles);
            return;
        }
        
        if (waterTiles.Count == 2)
        {
            Rot4 dir0 = waterTiles[0].AsRot4();
            Rot4 dir1 = waterTiles[1].AsRot4();

            if (dir0 == dir1)
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CoastOneSide;
                DetermineCliffTopology(info, waterTiles[0], cliffTiles);
                return;
            }

            if (waterTiles[1] == waterTiles[0].Rotated(RotationDirection.Clockwise))
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CoastTwoSides;
                return;
            }
            
            if (waterTiles[1] == waterTiles[0].Rotated(RotationDirection.Counterclockwise))
            {
                info.LandformDirection = dir1;
                info.Topology = Topology.CoastTwoSides;
                return;
            }

            Rot6 random = waterTiles.Random(info.MakeSeed(1785));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CoastOneSide;
            DetermineCliffTopology(info, random, cliffTiles);
            return;
        }
        
        if (waterTiles.Count == 3)
        {
            Rot6 middle = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.RotatedCW()) && waterTiles.Contains(d.RotatedCCW()), Rot6.Invalid);
            if (middle.IsValid)
            {
                DetermineCoastTopology(info, middle, 0.25f);
                return;
            }
            
            Rot6 first = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.RotatedCW()), Rot6.Invalid);
            if (first.IsValid)
            {
                if (first.AsRot4() == first.RotatedCW().AsRot4())
                {
                    info.LandformDirection = first.AsRot4();
                    info.Topology = Topology.CoastOneSide;
                    return;
                }

                info.LandformDirection = first.AsRot4();
                info.Topology = Topology.CoastTwoSides;
                return;
            }
            
            info.Topology = Topology.Inland;
            return;
        }

        var landTiles = Rot6.All.Except(waterTiles).ToList();
        
        if (waterTiles.Count == 4)
        {
            if (landTiles[0] == landTiles[1].RotatedCW() || landTiles[0] == landTiles[1].RotatedCCW())
            {
                DetermineCoastTopology(info, landTiles.Random(info.MakeSeed(9365)).Opposite, 0.5f);
                return;
            }
            
            if (landTiles[0] == landTiles[1].Opposite)
            {
                info.LandformDirection = landTiles[0].Opposite.AsRot4();
                info.Topology = Topology.CoastLandbridge;
                return;
            }
            
            Rot6 middle = waterTiles.FirstOrFallback(d => waterTiles.Contains(d.RotatedCW()) && waterTiles.Contains(d.RotatedCCW()), Rot6.Invalid);
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

    public static void DetermineCoastTopology(WorldTileInfo info, Rot6 dir, float chance)
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

        info.LandformDirection = Rand.ChanceSeeded(0.5f, info.MakeSeed(9777)) ? dir.AsRot4() : dir.RotatedCCW().AsRot4();
        info.Topology = Topology.CoastTwoSides;
    }

    public static void DetermineCliffTopology(WorldTileInfo info, Rot6 coastDir, List<Rot6> cliffTiles)
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
            Rot4 dir0 = cliffTiles[0].AsRot4();
            Rot4 dir1 = cliffTiles[1].AsRot4();

            if (dir0 == dir1)
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CliffOneSide;
                return;
            }

            if (cliffTiles[1] == cliffTiles[0].Rotated(RotationDirection.Clockwise))
            {
                info.LandformDirection = dir0;
                info.Topology = Topology.CliffTwoSides;
                return;
            }
            
            if (cliffTiles[1] == cliffTiles[0].Rotated(RotationDirection.Counterclockwise))
            {
                info.LandformDirection = dir1;
                info.Topology = Topology.CliffTwoSides;
                return;
            }

            Rot6 random = cliffTiles.Random(info.MakeSeed(2573));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }
        
        if (cliffTiles.Count == 3)
        {
            Rot6 first = cliffTiles.Where(d => cliffTiles.Contains(d.RotatedCW()) && d.AsRot4() != d.RotatedCW().AsRot4()).ToList().Random(info.MakeSeed(2647));
            if (first.IsValid)
            {
                info.LandformDirection = first.AsRot4();
                info.Topology = Topology.CliffTwoSides;
                return;
            }
            
            Rot6 random = cliffTiles.Random(info.MakeSeed(1142));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }
        
        var nonCliffTiles = Rot6.All.Except(cliffTiles).ToList();
        
        if (cliffTiles.Count == 4)
        {
            if (nonCliffTiles[0] == nonCliffTiles[1].RotatedCW() || nonCliffTiles[0] == nonCliffTiles[1].RotatedCCW())
            {
                info.LandformDirection = nonCliffTiles.Random(info.MakeSeed(1455)).AsRot4().Opposite;
                info.Topology = Topology.CliffThreeSides;
                return;
            }
            
            if (nonCliffTiles[0] == nonCliffTiles[1].Opposite)
            {
                info.LandformDirection = nonCliffTiles[0].AsRot4();
                info.Topology = Topology.CliffValley;
                return;
            }
            
            Rot6 first = cliffTiles.Where(d => cliffTiles.Contains(d.RotatedCW()) && d.AsRot4() != d.RotatedCW().AsRot4()).ToList().Random(info.MakeSeed(1675));
            if (first.IsValid)
            {
                info.LandformDirection = first.AsRot4();
                info.Topology = Topology.CliffTwoSides;
                return;
            }
            
            Rot6 random = cliffTiles.Random(info.MakeSeed(1675));
            info.LandformDirection = random.AsRot4();
            info.Topology = Topology.CliffOneSide;
            return;
        }
        
        if (cliffTiles.Count == 5)
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
}
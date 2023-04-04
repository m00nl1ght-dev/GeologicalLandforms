using System;
using System.Collections.Generic;
using LunarFramework.Utility;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms;

public class TunnelGenerator
{
    public float OpenTunnelsPer10K = 5.8f;
    public float ClosedTunnelsPer10K = 2.5f;

    public int MaxOpenTunnelsPerRockGroup = 3;
    public int MaxClosedTunnelsPerRockGroup = 1;

    public float TunnelWidthMultiplierMin = 0.8f;
    public float TunnelWidthMultiplierMax = 1f;

    public float TunnelWidthMin = 1.4f;
    public float WidthReductionPerCell = 0.034f;

    public float BranchChance = 0.1f;
    public int BranchMinDistanceFromStart = 15;
    public float BranchWidthOffsetMultiplier = 1f;

    public float DirectionChangeSpeed = 8f;

    public float DirectionNoiseFrequency = 0.00205f;
    public int MinRocksToGenerateAnyTunnel = 300;

    public FloatRange BranchedTunnelWidthOffset = new(0.2f, 0.4f);
    public SimpleCurve TunnelsWidthPerRockCount = new() { new(100f, 2f), new(300f, 4f), new(3000f, 5.5f) };

    [ThreadStatic]
    private static HashSet<IntVec3> GroupSet;

    [ThreadStatic]
    private static HashSet<IntVec3> GroupVisited;

    [ThreadStatic]
    private static List<IntVec3> SubGroup;

    private bool[,] _baseGrid;
    private double[,] _cavesGrid;
    private double[,] _depthGrid;
    private double[,] _offsetGrid;

    private ModuleBase _directionNoise;
    private RandInstance _rand;

    public Result Generate(IntVec2 gridSize, RandInstance rand, Predicate<IntVec2> cellCondition, bool depthGrid = false, bool offsetGrid = false)
    {
        if (GroupSet == null)
        {
            GroupSet = new();
            GroupVisited = new();
            SubGroup = new();
        }

        var map = new Map { info = new MapInfo { Size = new IntVec3(gridSize.x, 1, gridSize.z) } };

        map.cellIndices = new CellIndices(map);
        map.floodFiller = new FloodFiller(map);

        _rand = rand;
        _directionNoise = new Perlin(DirectionNoiseFrequency, 2.0, 0.5, 4, rand.Int, QualityMode.Medium);

        _baseGrid = new bool[map.Size.x, map.Size.z];
        _cavesGrid = new double[map.Size.x, map.Size.z];
        _depthGrid = depthGrid ? new double[map.Size.x, map.Size.z] : null;
        _offsetGrid = offsetGrid ? new double[map.Size.x, map.Size.z] : null;
        

        var visited = new BoolGrid(map);
        var group = new List<IntVec3>();
        var groupSet = new HashSet<IntVec3>();

        foreach (var c in map.AllCells)
        {
            _baseGrid[c.x, c.z] = cellCondition.Invoke(new IntVec2(c.x, c.z));
        }

        foreach (var c in map.AllCells)
        {
            if (!visited[c] && MatchesCellCondition(c, map))
            {
                group.Clear();
                groupSet.Clear();

                map.floodFiller.FloodFill(c, x => MatchesCellCondition(x, map), x =>
                {
                    visited[x] = true;
                    group.Add(x);
                });

                Trim(group, map);
                RemoveSmallDisconnectedSubGroups(group, map);

                if (group.Count >= MinRocksToGenerateAnyTunnel)
                {
                    groupSet.AddRange(group);

                    DoOpenTunnels(group, groupSet, map);
                    DoClosedTunnels(group, groupSet, map);
                }
            }
        }

        var result = new Result
        {
            BaseGrid = _baseGrid,
            CavesGrid = _cavesGrid,
            DepthGrid = _depthGrid,
            OffsetGrid = _offsetGrid
        };

        _rand = null;
        _directionNoise = null;
        
        _baseGrid = null;
        _cavesGrid = null;
        _depthGrid = null;
        _offsetGrid = null;

        return result;
    }

    public struct Result
    {
        public bool[,] BaseGrid;
        public double[,] CavesGrid;
        public double[,] DepthGrid;
        public double[,] OffsetGrid;
    }

    private void Trim(List<IntVec3> group, Map map) => GenMorphology.Open(group, 6, map);

    private bool MatchesCellCondition(IntVec3 c, Map map) => c.InBounds(map) && _baseGrid[c.x, c.z];

    private void DoOpenTunnels(List<IntVec3> group, HashSet<IntVec3> groupSet, Map map)
    {
        int count = Mathf.Min(_rand.RoundRandom((float) (group.Count * (double) _rand.Range(0.9f, 1.1f) * OpenTunnelsPer10K / 10000.0)), MaxOpenTunnelsPerRockGroup);
        if (count > 0) count = _rand.RangeInclusive(1, count);

        float desiredWidth = TunnelsWidthPerRockCount.Evaluate(group.Count);

        var edgeCells = FindEdgeCells(group, groupSet, map);

        for (int i = 0; i < count; ++i)
        {
            var start = IntVec3.Invalid;
            float num1 = -1f;
            float dir = -1f;
            float num2 = -1f;

            for (int j = 0; j < 10; ++j)
            {
                var edgeCellForTunnel = FindRandomEdgeCellForTunnel(group, edgeCells);
                float distToCave = GetDistToCave(edgeCellForTunnel, groupSet, map, 40f, false);
                float bestInitialDir = FindBestInitialDir(edgeCellForTunnel, groupSet, out var dist);

                if (!start.IsValid || distToCave > (double) num1 || distToCave == (double) num1 && dist > (double) num2)
                {
                    start = edgeCellForTunnel;
                    num1 = distToCave;
                    dir = bestInitialDir;
                    num2 = dist;
                }
            }

            float width = _rand.Range(desiredWidth * TunnelWidthMultiplierMin, desiredWidth * TunnelWidthMultiplierMax);
            Dig(start, dir, width, 0f, group, groupSet, map, false);

            UpdateEdgeCells(edgeCells);
        }
    }

    private void DoClosedTunnels(List<IntVec3> group, HashSet<IntVec3> groupSet, Map map)
    {
        int count = Mathf.Min(_rand.RoundRandom((float) (group.Count * (double) _rand.Range(0.9f, 1.1f) * ClosedTunnelsPer10K / 10000.0)), MaxClosedTunnelsPerRockGroup);
        if (count > 0) count = _rand.RangeInclusive(0, count);

        float desiredWidth = TunnelsWidthPerRockCount.Evaluate(group.Count);

        for (int i = 0; i < count; ++i)
        {
            var start = IntVec3.Invalid;
            float num = -1f;

            for (int j = 0; j < 7; ++j)
            {
                var cell = _rand.RandomElement(group);
                float distToCave = GetDistToCave(cell, groupSet, map, 30f, true);

                if (!start.IsValid || distToCave > (double) num)
                {
                    start = cell;
                    num = distToCave;
                }
            }

            float width = _rand.Range(desiredWidth * TunnelWidthMultiplierMin, desiredWidth * TunnelWidthMultiplierMax);
            Dig(start, _rand.Range(0.0f, 360f), width, 0f, group, groupSet, map, true);
        }
    }

    private List<IntVec3> FindEdgeCells(List<IntVec3> group, HashSet<IntVec3> groupSet, Map map)
    {
        var cardinalDirections = GenAdj.CardinalDirections;

        var edgeCells = new List<IntVec3>();

        for (int index1 = 0; index1 < group.Count; ++index1)
        {
            if (group[index1].DistanceToEdge(map) >= 3 && _cavesGrid.Get(group[index1]) <= 0.0)
            {
                for (int index2 = 0; index2 < 4; ++index2)
                {
                    var intVec3 = group[index1] + cardinalDirections[index2];
                    if (!groupSet.Contains(intVec3))
                    {
                        edgeCells.Add(group[index1]);
                        break;
                    }
                }
            }
        }

        return edgeCells;
    }

    private void UpdateEdgeCells(List<IntVec3> edgeCells)
    {
        edgeCells.RemoveAll(c => _cavesGrid.Get(c) > 0.0);
    }

    private IntVec3 FindRandomEdgeCellForTunnel(List<IntVec3> group, List<IntVec3> edgeCells)
    {
        if (edgeCells.Any()) return _rand.RandomElement(edgeCells);

        Log.Warning("Could not find any valid edge cell.");
        return _rand.RandomElement(group);
    }

    private float FindBestInitialDir(IntVec3 start, HashSet<IntVec3> groupSet, out float dist)
    {
        float distToNonRock1 = GetDistToNonRock(start, groupSet, IntVec3.East, 40);
        float distToNonRock2 = GetDistToNonRock(start, groupSet, IntVec3.West, 40);
        float distToNonRock3 = GetDistToNonRock(start, groupSet, IntVec3.South, 40);
        float distToNonRock4 = GetDistToNonRock(start, groupSet, IntVec3.North, 40);
        float distToNonRock5 = GetDistToNonRock(start, groupSet, IntVec3.NorthWest, 40);
        float distToNonRock6 = GetDistToNonRock(start, groupSet, IntVec3.NorthEast, 40);
        float distToNonRock7 = GetDistToNonRock(start, groupSet, IntVec3.SouthWest, 40);
        float distToNonRock8 = GetDistToNonRock(start, groupSet, IntVec3.SouthEast, 40);

        dist = Mathf.Max(distToNonRock1, distToNonRock2, distToNonRock3, distToNonRock4, distToNonRock5, distToNonRock6, distToNonRock7, distToNonRock8);

        return _rand.MaxByRandomIfEqual(0.0f,
            (float) (distToNonRock1 + distToNonRock8 / 2.0 + distToNonRock6 / 2.0), 45f,
            (float) (distToNonRock8 + distToNonRock3 / 2.0 + distToNonRock1 / 2.0), 90f,
            (float) (distToNonRock3 + distToNonRock8 / 2.0 + distToNonRock7 / 2.0), 135f,
            (float) (distToNonRock7 + distToNonRock3 / 2.0 + distToNonRock2 / 2.0), 180f,
            (float) (distToNonRock2 + distToNonRock7 / 2.0 + distToNonRock5 / 2.0), 225f,
            (float) (distToNonRock5 + distToNonRock4 / 2.0 + distToNonRock2 / 2.0), 270f,
            (float) (distToNonRock4 + distToNonRock6 / 2.0 + distToNonRock5 / 2.0), 315f,
            (float) (distToNonRock6 + distToNonRock4 / 2.0 + distToNonRock1 / 2.0));
    }

    private void Dig(
        IntVec3 start, float dir, float width, float distFromRoot,
        List<IntVec3> group, HashSet<IntVec3> groupSet, Map map,
        bool closed, HashSet<IntVec3> visited = null)
    {
        var pos = start.ToVector3Shifted();
        var cell = start;
        
        float num1 = 0.0f;

        bool branchedRight = false;
        bool branchedLeft = false;
        
        visited ??= new HashSet<IntVec3>();

        int distFromStart = 0;
        
        while (true)
        {
            if (closed)
            {
                int cellsInRadius = GenRadial.NumCellsInRadius((float) (width / 2.0 + 1.5));
                for (int index = 0; index < cellsInRadius; ++index)
                {
                    var c = cell + GenRadial.RadialPattern[index];
                    if (!visited.Contains(c) && (!groupSet.Contains(c) || _cavesGrid.Get(c) > 0.0)) return;
                }
            }

            if (distFromStart >= BranchMinDistanceFromStart && width > TunnelWidthMin + BranchedTunnelWidthOffset.max * BranchWidthOffsetMultiplier)
            {
                if (!branchedRight && _rand.Chance(BranchChance))
                {
                    DigInBestDirection(cell, dir, new FloatRange(40f, 90f),
                        width - _rand.Range(BranchedTunnelWidthOffset.min, BranchedTunnelWidthOffset.max) * BranchWidthOffsetMultiplier,
                        distFromRoot + distFromStart - width, group, groupSet, map, closed, visited);
                    branchedRight = true;
                }

                if (!branchedLeft && _rand.Chance(BranchChance))
                {
                    DigInBestDirection(cell, dir, new FloatRange(-90f, -40f),
                        width - _rand.Range(BranchedTunnelWidthOffset.min, BranchedTunnelWidthOffset.max) * BranchWidthOffsetMultiplier,
                        distFromRoot + distFromStart - width, group, groupSet, map, closed, visited);
                    branchedLeft = true;
                }
            }

            SetCaveAround(cell, width, distFromRoot + distFromStart, dir, map, visited, out var hitAnotherTunnel);

            if (hitAnotherTunnel) return;

            while (pos.ToIntVec3() == cell)
            {
                pos += Vector3Utility.FromAngleFlat(dir) * 0.5f;
                num1 += 0.5f;
            }

            if (!groupSet.Contains(pos.ToIntVec3())) return;

            var tempCell = new IntVec3(cell.x, 0, pos.ToIntVec3().z);

            if (MatchesCellCondition(tempCell, map))
            {
                _cavesGrid.Set(tempCell, Math.Max(_cavesGrid.Get(tempCell), width));
                visited.Add(tempCell);
            }

            cell = pos.ToIntVec3();
            dir += (float) _directionNoise.GetValue(num1 * 60.0, start.x * 200.0, start.z * 200.0) * DirectionChangeSpeed;
            width -= WidthReductionPerCell;

            if (width >= TunnelWidthMin) ++distFromStart;
            else return;
        }
    }

    private void DigInBestDirection(
        IntVec3 curIntVec, float curDir, FloatRange dirOffset, float width, float distFromRoot,
        List<IntVec3> group, HashSet<IntVec3> groupSet, Map map,
        bool closed, HashSet<IntVec3> visited = null)
    {
        int num = -1;
        float dir1 = -1f;

        for (int index = 0; index < 6; ++index)
        {
            float dir2 = curDir + _rand.Range(dirOffset.min, dirOffset.max);
            int distToNonRock = GetDistToNonRock(curIntVec, groupSet, dir2, 50);

            if (distToNonRock > num)
            {
                num = distToNonRock;
                dir1 = dir2;
            }
        }

        if (num < 18) return;
        Dig(curIntVec, dir1, width, distFromRoot, group, groupSet, map, closed, visited);
    }

    private void SetCaveAround(
        IntVec3 around, float tunnelWidth, float distFromRoot, float dir,
        Map map, HashSet<IntVec3> visited, out bool hitAnotherTunnel)
    {
        hitAnotherTunnel = false;

        int cellsInRadius = GenRadial.NumCellsInRadius(tunnelWidth / 2f);

        for (int i = 0; i < cellsInRadius; ++i)
        {
            var c = around + GenRadial.RadialPattern[i];
            
            if (MatchesCellCondition(c, map))
            {
                if (_cavesGrid.Get(c) > 0.0 && !visited.Contains(c)) hitAnotherTunnel = true;
                _cavesGrid.Set(c, Math.Max(_cavesGrid.Get(c), tunnelWidth));
                visited.Add(c);
            }
        }

        if (_depthGrid != null || _offsetGrid != null)
        {
            int cellsInRadiusExtra = GenRadial.NumCellsInRadius(tunnelWidth / 2f + 2f);

            for (int i = 0; i < cellsInRadiusExtra; ++i)
            {
                var p = GenRadial.RadialPattern[i];
                var c = around + p;

                if (c.InBounds(map))
                {
                    var vec = p.ToVector3().RotatedBy(-dir);
                    _depthGrid?.Set(c, distFromRoot + vec.x - tunnelWidth / 2f);
                    _offsetGrid?.Set(c, vec.z);
                }
            }
        }
    }

    private int GetDistToNonRock(IntVec3 from, HashSet<IntVec3> groupSet, IntVec3 offset, int maxDist)
    {
        for (int distToNonRock = 0; distToNonRock <= maxDist; ++distToNonRock)
        {
            var intVec3 = from + offset * distToNonRock;
            if (!groupSet.Contains(intVec3)) return distToNonRock;
        }

        return maxDist;
    }

    private int GetDistToNonRock(IntVec3 from, HashSet<IntVec3> groupSet, float dir, int maxDist)
    {
        var vector3 = Vector3Utility.FromAngleFlat(dir);
        for (int distToNonRock = 0; distToNonRock <= maxDist; ++distToNonRock)
        {
            var intVec3 = (from.ToVector3Shifted() + vector3 * distToNonRock).ToIntVec3();
            if (!groupSet.Contains(intVec3)) return distToNonRock;
        }

        return maxDist;
    }

    private float GetDistToCave(
        IntVec3 cell, HashSet<IntVec3> groupSet,
        Map map, float maxDist, bool treatOpenSpaceAsCave)
    {
        int num = GenRadial.NumCellsInRadius(maxDist);
        var radialPattern = GenRadial.RadialPattern;

        for (int index = 0; index < num; ++index)
        {
            var intVec3 = cell + radialPattern[index];
            if (treatOpenSpaceAsCave && !groupSet.Contains(intVec3) || intVec3.InBounds(map) && _cavesGrid.Get(intVec3) > 0.0)
                return cell.DistanceTo(intVec3);
        }

        return maxDist;
    }

    private void RemoveSmallDisconnectedSubGroups(List<IntVec3> group, Map map)
    {
        GroupSet.Clear();
        GroupSet.AddRange(group);
        GroupVisited.Clear();

        foreach (var t in group)
        {
            if (!GroupVisited.Contains(t) && GroupSet.Contains(t))
            {
                SubGroup.Clear();

                map.floodFiller.FloodFill(t, x => GroupSet.Contains(x), x =>
                {
                    SubGroup.Add(x);
                    GroupVisited.Add(x);
                });

                if (SubGroup.Count < MinRocksToGenerateAnyTunnel || SubGroup.Count < 0.05 * group.Count)
                {
                    foreach (var t1 in SubGroup) GroupSet.Remove(t1);
                }
            }
        }

        group.Clear();
        group.AddRange(GroupSet);
    }
}

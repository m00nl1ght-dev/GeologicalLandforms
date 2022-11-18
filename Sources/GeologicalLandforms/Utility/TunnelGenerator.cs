using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace GeologicalLandforms;

public class TunnelGenerator
{
    private ModuleBase _directionNoise;
    
    public float OpenTunnelsPer10K = 5.8f;
    public float ClosedTunnelsPer10K = 2.5f;
    public int MaxOpenTunnelsPerRockGroup = 3;
    public int MaxClosedTunnelsPerRockGroup = 1;
    public float DirectionChangeSpeed = 8f;
    public float DirectionNoiseFrequency = 0.00205f;
    public int MinRocksToGenerateAnyTunnel = 300;
    public int AllowBranchingAfterThisManyCells = 15;
    public float MinTunnelWidth = 1.4f;
    public float WidthOffsetPerCell = 0.034f;
    public float BranchChance = 0.1f;
    
    public FloatRange BranchedTunnelWidthOffset = new(0.2f, 0.4f);
    public SimpleCurve TunnelsWidthPerRockCount = new() { new(100f, 2f), new(300f, 4f), new(3000f, 5.5f) };
    
    private static readonly HashSet<IntVec3> TmpGroupSet = new();
    private static readonly List<IntVec3> TmpCells = new();
    private static readonly HashSet<IntVec3> GroupSet = new();
    private static readonly HashSet<IntVec3> GroupVisited = new();
    private static readonly List<IntVec3> SubGroup = new();

    public void Generate(Map map)
    {
        _directionNoise = new Perlin(DirectionNoiseFrequency, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
        
        var elevation = MapGenerator.Elevation;
        var visited = new BoolGrid(map);
        var group = new List<IntVec3>();
        
        foreach (var allCell in map.AllCells)
        {
            if (!visited[allCell] && IsRock(allCell, elevation, map))
            {
                group.Clear();
                
                map.floodFiller.FloodFill(allCell, x => IsRock(x, elevation, map), x =>
                {
                    visited[x] = true;
                    group.Add(x);
                });
                
                Trim(group, map);
                RemoveSmallDisconnectedSubGroups(group, map);
                
                if (group.Count >= MinRocksToGenerateAnyTunnel)
                {
                    DoOpenTunnels(group, map);
                    DoClosedTunnels(group, map);
                }
            }
        }
    }

    private void Trim(List<IntVec3> group, Map map) => GenMorphology.Open(group, 6, map);

    private bool IsRock(IntVec3 c, MapGenFloatGrid elevation, Map map) => c.InBounds(map) && elevation[c] > 0.7;

    private void DoOpenTunnels(List<IntVec3> group, Map map)
    {
        int max1 = Mathf.Min(GenMath.RoundRandom((float) (group.Count * (double) Rand.Range(0.9f, 1.1f) * OpenTunnelsPer10K / 10000.0)), MaxOpenTunnelsPerRockGroup);
        if (max1 > 0) max1 = Rand.RangeInclusive(1, max1);
        
        float max2 = TunnelsWidthPerRockCount.Evaluate(group.Count);
        
        for (int index1 = 0; index1 < max1; ++index1)
        {
            var start = IntVec3.Invalid;
            float num1 = -1f;
            float dir = -1f;
            float num2 = -1f;
            
            for (int index2 = 0; index2 < 10; ++index2)
            {
                var edgeCellForTunnel = FindRandomEdgeCellForTunnel(group, map);
                float distToCave = GetDistToCave(edgeCellForTunnel, group, map, 40f, false);
                float bestInitialDir = FindBestInitialDir(edgeCellForTunnel, group, out var dist);
                
                if (!start.IsValid || distToCave > (double) num1 || distToCave == (double) num1 && dist > (double) num2)
                {
                    start = edgeCellForTunnel;
                    num1 = distToCave;
                    dir = bestInitialDir;
                    num2 = dist;
                }
            }
            
            float width = Rand.Range(max2 * 0.8f, max2);
            Dig(start, dir, width, group, map, false);
        }
    }

    private void DoClosedTunnels(List<IntVec3> group, Map map)
    {
        int max1 = Mathf.Min(GenMath.RoundRandom((float) (group.Count * (double) Rand.Range(0.9f, 1.1f) * ClosedTunnelsPer10K / 10000.0)), MaxClosedTunnelsPerRockGroup);
        if (max1 > 0) max1 = Rand.RangeInclusive(0, max1);
        
        float max2 = TunnelsWidthPerRockCount.Evaluate(group.Count);
        
        for (int index1 = 0; index1 < max1; ++index1)
        {
            var start = IntVec3.Invalid;
            float num = -1f;
            
            for (int index2 = 0; index2 < 7; ++index2)
            {
                var cell = group.RandomElement();
                float distToCave = GetDistToCave(cell, group, map, 30f, true);
                
                if (!start.IsValid || distToCave > (double) num)
                {
                    start = cell;
                    num = distToCave;
                }
            }
            
            float width = Rand.Range(max2 * 0.8f, max2);
            Dig(start, Rand.Range(0.0f, 360f), width, group, map, true);
        }
    }

    private IntVec3 FindRandomEdgeCellForTunnel(List<IntVec3> group, Map map)
    {
        var caves = MapGenerator.Caves;
        var cardinalDirections = GenAdj.CardinalDirections;
        
        TmpCells.Clear();
        TmpGroupSet.Clear();
        TmpGroupSet.AddRange(group);
        
        for (int index1 = 0; index1 < group.Count; ++index1)
        {
            if (group[index1].DistanceToEdge(map) >= 3 && caves[group[index1]] <= 0.0)
            {
                for (int index2 = 0; index2 < 4; ++index2)
                {
                    var intVec3 = group[index1] + cardinalDirections[index2];
                    if (!TmpGroupSet.Contains(intVec3))
                    {
                        TmpCells.Add(group[index1]);
                        break;
                    }
                }
            }
        }
        
        if (TmpCells.Any()) return TmpCells.RandomElement();
        
        Log.Warning("Could not find any valid edge cell.");
        return group.RandomElement();
    }

    private float FindBestInitialDir(IntVec3 start, List<IntVec3> group, out float dist)
    {
        float distToNonRock1 = GetDistToNonRock(start, group, IntVec3.East, 40);
        float distToNonRock2 = GetDistToNonRock(start, group, IntVec3.West, 40);
        float distToNonRock3 = GetDistToNonRock(start, group, IntVec3.South, 40);
        float distToNonRock4 = GetDistToNonRock(start, group, IntVec3.North, 40);
        float distToNonRock5 = GetDistToNonRock(start, group, IntVec3.NorthWest, 40);
        float distToNonRock6 = GetDistToNonRock(start, group, IntVec3.NorthEast, 40);
        float distToNonRock7 = GetDistToNonRock(start, group, IntVec3.SouthWest, 40);
        float distToNonRock8 = GetDistToNonRock(start, group, IntVec3.SouthEast, 40);
        
        dist = Mathf.Max(distToNonRock1, distToNonRock2, distToNonRock3, distToNonRock4, distToNonRock5, distToNonRock6, distToNonRock7, distToNonRock8);
        
        return GenMath.MaxByRandomIfEqual(0.0f, 
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
        IntVec3 start,
        float dir, float width,
        List<IntVec3> group,
        Map map, bool closed,
        HashSet<IntVec3> visited = null)
    {
        var vector3Shifted = start.ToVector3Shifted();
        var intVec3 = start;
        float num1 = 0.0f;
        
        var elevation = MapGenerator.Elevation;
        var caves = MapGenerator.Caves;
        
        bool flag1 = false;
        bool flag2 = false;
        visited ??= new HashSet<IntVec3>();
        
        TmpGroupSet.Clear();
        TmpGroupSet.AddRange(group);
        
        int num2 = 0;
        while (true)
        {
            if (closed)
            {
                int num3 = GenRadial.NumCellsInRadius((float) (width / 2.0 + 1.5));
                for (int index = 0; index < num3; ++index)
                {
                    var c = intVec3 + GenRadial.RadialPattern[index];
                    if (!visited.Contains(c) && (!TmpGroupSet.Contains(c) || caves[c] > 0.0)) return;
                }
            }
            
            if (num2 >= AllowBranchingAfterThisManyCells && width > MinTunnelWidth + BranchedTunnelWidthOffset.max)
            {
                if (!flag1 && Rand.Chance(BranchChance))
                {
                    DigInBestDirection(intVec3, dir, new FloatRange(40f, 90f), width - BranchedTunnelWidthOffset.RandomInRange, group, map, closed, visited);
                    flag1 = true;
                }
                
                if (!flag2 && Rand.Chance(BranchChance))
                {
                    DigInBestDirection(intVec3, dir, new FloatRange(-90f, -40f), width - BranchedTunnelWidthOffset.RandomInRange, group, map, closed, visited);
                    flag2 = true;
                }
            }

            SetCaveAround(intVec3, width, map, visited, out var hitAnotherTunnel);
            
            if (!hitAnotherTunnel)
            {
                while (vector3Shifted.ToIntVec3() == intVec3)
                {
                    vector3Shifted += Vector3Utility.FromAngleFlat(dir) * 0.5f;
                    num1 += 0.5f;
                }
                
                if (TmpGroupSet.Contains(vector3Shifted.ToIntVec3()))
                {
                    var c = new IntVec3(intVec3.x, 0, vector3Shifted.ToIntVec3().z);
                    
                    if (IsRock(c, elevation, map))
                    {
                        caves[c] = Mathf.Max(caves[c], width);
                        visited.Add(c);
                    }
                    
                    intVec3 = vector3Shifted.ToIntVec3();
                    dir += (float) _directionNoise.GetValue(num1 * 60.0, start.x * 200.0, start.z * 200.0) * DirectionChangeSpeed;
                    width -= WidthOffsetPerCell;
                    
                    if (width >= MinTunnelWidth) ++num2;
                    else return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    private void DigInBestDirection(
        IntVec3 curIntVec, float curDir,
        FloatRange dirOffset, float width,
        List<IntVec3> group, Map map, bool closed,
        HashSet<IntVec3> visited = null)
    {
        int num = -1;
        float dir1 = -1f;
        
        for (int index = 0; index < 6; ++index)
        {
            float dir2 = curDir + dirOffset.RandomInRange;
            int distToNonRock = GetDistToNonRock(curIntVec, group, dir2, 50);
            
            if (distToNonRock > num)
            {
                num = distToNonRock;
                dir1 = dir2;
            }
        }
        
        if (num < 18) return;
        Dig(curIntVec, dir1, width, group, map, closed, visited);
    }

    private void SetCaveAround(
        IntVec3 around, float tunnelWidth,
        Map map, HashSet<IntVec3> visited,
        out bool hitAnotherTunnel)
    {
        hitAnotherTunnel = false;
        int num = GenRadial.NumCellsInRadius(tunnelWidth / 2f);
        
        var elevation = MapGenerator.Elevation;
        var caves = MapGenerator.Caves;
        
        for (int index = 0; index < num; ++index)
        {
            var c = around + GenRadial.RadialPattern[index];
            if (IsRock(c, elevation, map))
            {
                if (caves[c] > 0.0 && !visited.Contains(c)) hitAnotherTunnel = true;
                caves[c] = Mathf.Max(caves[c], tunnelWidth);
                visited.Add(c);
            }
        }
    }

    private int GetDistToNonRock(IntVec3 from, List<IntVec3> group, IntVec3 offset, int maxDist)
    {
        GroupSet.Clear();
        GroupSet.AddRange(group);
        
        for (int distToNonRock = 0; distToNonRock <= maxDist; ++distToNonRock)
        {
            var intVec3 = from + offset * distToNonRock;
            if (!GroupSet.Contains(intVec3)) return distToNonRock;
        }
        
        return maxDist;
    }

    private int GetDistToNonRock(IntVec3 from, List<IntVec3> group, float dir, int maxDist)
    {
        GroupSet.Clear();
        GroupSet.AddRange(group);
        
        var vector3 = Vector3Utility.FromAngleFlat(dir);
        for (int distToNonRock = 0; distToNonRock <= maxDist; ++distToNonRock)
        {
            var intVec3 = (from.ToVector3Shifted() + vector3 * distToNonRock).ToIntVec3();
            if (!GroupSet.Contains(intVec3)) return distToNonRock;
        }
        
        return maxDist;
    }

    private float GetDistToCave(
        IntVec3 cell, List<IntVec3> group,
        Map map, float maxDist,
        bool treatOpenSpaceAsCave)
    {
        var caves = MapGenerator.Caves;
        
        TmpGroupSet.Clear();
        TmpGroupSet.AddRange(group);
        
        int num = GenRadial.NumCellsInRadius(maxDist);
        var radialPattern = GenRadial.RadialPattern;
        
        for (int index = 0; index < num; ++index)
        {
            var intVec3 = cell + radialPattern[index];
            if (treatOpenSpaceAsCave && !TmpGroupSet.Contains(intVec3) || intVec3.InBounds(map) && caves[intVec3] > 0.0)
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
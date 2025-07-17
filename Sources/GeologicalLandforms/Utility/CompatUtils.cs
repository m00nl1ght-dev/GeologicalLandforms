using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using RimWorld.Planet;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public static class CompatUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length<T>(this NativeArray<T> array) where T : struct => array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length<T>(this List<T> list) where T : struct => list.Count;

    #if RW_1_6_OR_GREATER

    public static NativeArray<PlanetTile> ExtNbValues(this WorldGrid grid) => grid.UnsafeTileIDToNeighbors_values;
    public static NativeArray<int> ExtNbOffsets(this WorldGrid grid) => grid.UnsafeTileIDToNeighbors_offsets;
    public static NativeArray<Vector3> ExtVertValues(this WorldGrid grid) => grid.UnsafeVerts;
    public static NativeArray<int> ExtVertOffsets(this WorldGrid grid) => grid.UnsafeTileIDToVerts_offsets;

    #else

    public static List<int> ExtNbValues(this WorldGrid grid) => grid.tileIDToNeighbors_values;
    public static List<int> ExtNbOffsets(this WorldGrid grid) => grid.tileIDToNeighbors_offsets;
    public static List<Vector3> ExtVertValues(this WorldGrid grid) => grid.verts;
    public static List<int> ExtVertOffsets(this WorldGrid grid) => grid.tileIDToVerts_offsets;

    #endif

    public static bool IsMainOrLongEventThread()
    {
        if (UnityData.IsInMainThread)
            return true;

        if (LongEventHandler.eventThread == null)
            return false;

        return Thread.CurrentThread.ManagedThreadId == LongEventHandler.eventThread.ManagedThreadId;
    }
}

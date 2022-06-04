using System;
using Verse;

namespace GeologicalLandforms;

public struct StructRot6<T>
{
    public T North;
    public T NorthEast;
    public T SouthEast;
    public T South;
    public T SouthWest;
    public T NorthWest;
    
    public StructRot6(T val = default)
    {
        North = val;
        NorthEast = val;
        SouthEast = val;
        South = val;
        SouthWest = val;
        NorthWest = val;
    }
    
    public T this[Rot6 rot]
    {
        get
        {
            if (rot == Rot6.North) return North;
            if (rot == Rot6.NorthEast) return NorthEast;
            if (rot == Rot6.SouthEast) return SouthEast;
            if (rot == Rot6.South) return South;
            if (rot == Rot6.SouthWest) return SouthWest;
            if (rot == Rot6.NorthWest) return NorthWest;
            return default;
        }

        set
        {
            if (rot == Rot6.North) North = value;
            if (rot == Rot6.NorthEast) NorthEast = value;
            if (rot == Rot6.SouthEast) SouthEast = value;
            if (rot == Rot6.South) South = value;
            if (rot == Rot6.SouthWest) SouthWest = value;
            if (rot == Rot6.NorthWest) NorthWest = value;
        }
    }

    public T AtRot4(Rot4 rot, Func<T, T, T> merger)
    {
        if (rot == Rot4.North) return North;
        if (rot == Rot4.East) return merger(NorthEast, SouthEast);
        if (rot == Rot4.South) return South;
        if (rot == Rot4.West) return merger(NorthWest, SouthWest);
        return default;
    }
}
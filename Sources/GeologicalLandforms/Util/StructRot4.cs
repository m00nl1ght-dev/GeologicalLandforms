using Verse;

namespace GeologicalLandforms;

public struct StructRot4<T>
{
    public T North;
    public T East;
    public T South;
    public T West;

    public StructRot4(T val = default)
    {
        North = val;
        East = val;
        South = val;
        West = val;
    }
    
    public T this[Rot4 rot]
    {
        get
        {
            if (rot == Rot4.North) return North;
            if (rot == Rot4.East) return East;
            if (rot == Rot4.South) return South;
            if (rot == Rot4.West) return West;
            return default;
        }

        set
        {
            if (rot == Rot4.North) North = value;
            if (rot == Rot4.East) East = value;
            if (rot == Rot4.South) South = value;
            if (rot == Rot4.West) West = value;
        }
    }
}
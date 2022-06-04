using System;
using Verse;

namespace GeologicalLandforms;

public struct Rot6 : IEquatable<Rot6>
{
    private byte _index;
    private float _angle;

    public bool IsValid => _index < 6;

    public static Rot6 Invalid => new() {_index = 66};

    public int Index
    {
        get => _index;
        set => _index = (byte) (value >= 0 ? value % 6 : value % 6 + 6);
    }
    
    public float Angle
    {
        get => _angle;
        set => _angle = value % 360f;
    }

    public Rot6(int index, float angle)
    {
        _index = (byte) (index >= 0 ? index % 6 : index % 6 + 6);
        _angle = angle % 360f;
    }

    public Rot6 Opposite
    {
        get
        {
            return Index switch
            {
                0 => new Rot6(3, _angle + 180f),
                1 => new Rot6(4, _angle + 180f),
                2 => new Rot6(5, _angle + 180f),
                3 => new Rot6(0, _angle + 180f),
                4 => new Rot6(1, _angle + 180f),
                5 => new Rot6(2, _angle + 180f),
                _ => new Rot6()
            };
        }
    }
    
    public void Rotate(RotationDirection rotDir)
    {
        if (rotDir == RotationDirection.Clockwise)
        {
            ++Index;
            Angle += 60f;
        }
        else if (rotDir == RotationDirection.Counterclockwise)
        {
            --Index;
            Angle -= 60f;
        }
    }

    public Rot6 Rotated(RotationDirection rotDir)
    {
        var rot6 = this;
        rot6.Rotate(rotDir);
        return rot6;
    }
    
    public Rot6 RotatedCW()
    {
        return Rotated(RotationDirection.Clockwise);
    }
    
    public Rot6 RotatedCCW()
    {
        return Rotated(RotationDirection.Counterclockwise);
    }

    public Rot4 AsRot4()
    {
        return Rot4.FromAngleFlat(Angle);
    }

    public bool Adjacent(Rot6 other)
    {
        return (Index + 1) % 6 == other.Index || (Index - 1) % 6 == other.Index;
    }

    public static bool operator ==(Rot6 a, Rot6 b) => a.Index == b.Index;

    public static bool operator !=(Rot6 a, Rot6 b) => a.Index != b.Index;

    public bool Equals(Rot6 other)
    {
        return _index == other._index;
    }

    public override bool Equals(object obj)
    {
        return obj is Rot6 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _index.GetHashCode();
    }

    public override string ToString()
    {
        return $"{nameof(Index)}: {Index}, {nameof(Angle)}: {Angle}";
    }

    public static float MidPoint(Rot6 a, Rot6 b)
    {
        return MidPoint(a.Angle, b.Angle);
    }
    
    public static float MidPoint(float a, float b)
    {
        if (a > b) (a, b) = (b, a);
        if (b - a > 180) b -= 360;
        var f = (b + a) / 2;
        if (f < 0) f += 360;
        return f;
    }
}
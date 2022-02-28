using System;
using System.Collections.Generic;
using Verse;

namespace GeologicalLandforms;

public struct Rot6 : IEquatable<Rot6>
{
    public static readonly IReadOnlyList<Rot6> All = new List<Rot6> { North, NorthEast, SouthEast, South, SouthWest, NorthWest };

    private byte _rotInt;

    public bool IsValid => _rotInt < 100;

    public byte AsByte
    {
        get => _rotInt;
        set => _rotInt = (byte) (value % 6U);
    }

    public int AsInt
    {
        get => _rotInt;
        set
        {
            if (value < 0)
                value += 6000;
            _rotInt = (byte) (value % 6);
        }
    }

    public float AsAngle
    {
        get
        {
            return AsInt switch
            {
                0 => 0.0f,
                1 => 60f,
                2 => 120f,
                3 => 180f,
                4 => 240f,
                5 => 300f,
                _ => 0.0f
            };
        }
    }
    
    public static Rot6 North => new(0);

    public static Rot6 NorthEast => new(1);
    
    public static Rot6 SouthEast => new(2);

    public static Rot6 South => new(3);

    public static Rot6 SouthWest => new(4);
    
    public static Rot6 NorthWest => new(5);

    public static Rot6 Random => new(Rand.RangeInclusive(0, 5));

    public static Rot6 Invalid => new() { _rotInt = 200 };
    
    public Rot6(byte newRot) => _rotInt = newRot;

    public Rot6(int newRot) => _rotInt = (byte) (newRot % 6);
    
    public Rot6 Opposite
    {
        get
        {
            return AsInt switch
            {
                0 => new Rot6(3),
                1 => new Rot6(4),
                2 => new Rot6(5),
                3 => new Rot6(0),
                4 => new Rot6(1),
                5 => new Rot6(2),
                _ => new Rot6()
            };
        }
    }
    
    public void Rotate(RotationDirection rotDir)
    {
        if (rotDir == RotationDirection.Clockwise)
            ++AsInt;
        if (rotDir == RotationDirection.Counterclockwise) 
            --AsInt;
    }

    public Rot6 Rotated(RotationDirection rotDir)
    {
        Rot6 rot6 = this;
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
    
    public static Rot6 FromAngleFlat(float angle)
    {
        angle = GenMath.PositiveMod(angle, 360f);
        if (angle < 30.0)
            return North;
        if (angle < 90.0)
            return NorthEast;
        if (angle < 150.0)
            return SouthEast;
        if (angle < 210.0)
            return South;
        if (angle < 270.0)
            return SouthWest;
        if (angle < 330.0)
            return NorthWest;
        return North;
    }

    public Rot4 AsRot4()
    {
        return AsInt switch
        {
            0 => Rot4.North,
            1 => Rot4.East,
            2 => Rot4.East,
            3 => Rot4.South,
            4 => Rot4.West,
            5 => Rot4.West,
            _ => Rot4.Invalid
        };
    }

    public bool IsHorizontal()
    {
        return _rotInt is 0 or 3;
    }
    
    public bool IsVertical()
    {
        return IsValid && !IsHorizontal();
    }
    
    public static bool operator ==(Rot6 a, Rot6 b) => a.AsInt == b.AsInt;

    public static bool operator !=(Rot6 a, Rot6 b) => a.AsInt != b.AsInt;

    public bool Equals(Rot6 other)
    {
        return _rotInt == other._rotInt;
    }

    public override bool Equals(object obj)
    {
        return obj is Rot6 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _rotInt.GetHashCode();
    }
}
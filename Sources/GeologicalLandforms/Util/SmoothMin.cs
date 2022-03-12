using System;
using Verse.Noise;

namespace GeologicalLandforms;

public class SmoothMin : ModuleBase
{
    public readonly double Smoothness;
    
    public SmoothMin(ModuleBase lhs, ModuleBase rhs, double smoothness) : base(2)
    {
        modules[0] = lhs;
        modules[1] = rhs;
        Smoothness = smoothness;
    }

    public override double GetValue(double x, double y, double z)
    {
        var valA = modules[0].GetValue(x, y, z);
        var valB = modules[1].GetValue(x, y, z);

        return Of(valA, valB, Smoothness);
    }

    public static double Of(double a, double b, double smoothness)
    {
        if (smoothness <= 0f) return Math.Min(a, b);
        
        double max = Math.Max(a, b) * smoothness;
        double min = Math.Min(a, b) * smoothness;
        
        return (min - Math.Log(1f + Math.Exp(min - max))) / smoothness;
    }
}
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
        
        if (Smoothness <= 0f) return Math.Min(valA, valB);
        
        double max = Math.Max(valA, valB) * Smoothness;
        double min = Math.Min(valA, valB) * Smoothness;
        
        return (min - Math.Log(1f + Math.Exp(min - max))) / Smoothness;
    }
}
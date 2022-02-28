using System;
using Verse.Noise;

namespace GeologicalLandforms;

public class BiasedDistFromXZ : ModuleBase
{
    public readonly double SpanPositiveX;
    public readonly double SpanNegativeX;
    public readonly double SpanPositiveZ;
    public readonly double SpanNegativeZ;
    public readonly double CenterX;
    public readonly double CenterZ;
    public readonly double Bias;
    public readonly bool Radial;

    public BiasedDistFromXZ(float spanPositiveX, float spanNegativeX, float spanPositiveZ, float spanNegativeZ, float centerX, float centerZ, float bias, bool radial) : base(0)
    {
        SpanPositiveX = spanPositiveX;
        SpanNegativeX = spanNegativeX;
        SpanPositiveZ = spanPositiveZ;
        SpanNegativeZ = spanNegativeZ;
        CenterX = centerX;
        CenterZ = centerZ;
        Bias = bias;
        Radial = radial;
    }

    public override double GetValue(double x, double y, double z)
    {
        double diffX = x - CenterX;
        double diffZ = z - CenterZ;

        double spanX = diffX < 0 ? SpanNegativeX : SpanPositiveX;
        double spanZ = diffZ < 0 ? SpanNegativeZ : SpanPositiveZ;
        
        double valX = spanX == 0 ? 0 : Math.Abs(diffX) / spanX;
        double valZ = spanZ == 0 ? 0 : Math.Abs(diffZ) / spanZ;

        if (Radial)
        {
            return Bias + Math.Sqrt(Math.Pow(valX, 2) + Math.Pow(valZ, 2)) * (spanX < 0 || spanZ < 0 ? -1 : 1);
        }

        return Bias + valX + valZ;
    }
}
using System;
using System.Collections.Generic;

namespace TerrainGraph;

public static class GridFunction
{
    public static readonly Const<double> Zero = new(0f);
    public static readonly Const<double> One = new(1f);

    public static Const<T> Of<T>(T value) => new(value);
    
    public class Const<T> : IGridFunction<T>
    {
        public readonly T Value;

        public Const(T value)
        {
            Value = value;
        }

        public T ValueAt(double x, double z)
        {
            return Value;
        }
    }
    
    public class Select<T> : IGridFunction<T>
    {
        public readonly IGridFunction<double> Input;
        public readonly List<IGridFunction<T>> Options;
        public readonly List<double> Thresholds;
        public readonly Func<T, int, T> PostProcess;

        public Select(IGridFunction<double> input, List<IGridFunction<T>> options, List<double> thresholds, Func<T, int, T> postProcess = null)
        {
            Input = input;
            Options = options;
            Thresholds = thresholds;
            PostProcess = postProcess ?? ((v, _) => v);
        }

        public T ValueAt(double x, double z)
        {
            var value = Input.ValueAt(x, z);
            for (int i = 0; i < Math.Min(Thresholds.Count, Options.Count - 1); i++) 
                if (value < Thresholds[i]) return PostProcess(Options[i].ValueAt(x, z), i);
            return PostProcess(Options[Options.Count - 1].ValueAt(x, z), Options.Count - 1);
        }
    }

    public class Add : IGridFunction<double>
    {
        public readonly IGridFunction<double> A, B;

        public Add(IGridFunction<double> a, IGridFunction<double> b)
        {
            A = a;
            B = b;
        }

        public double ValueAt(double x, double z)
        {
            return A.ValueAt(x, z) + B.ValueAt(x, z);
        }
    }
    
    public class Multiply : IGridFunction<double>
    {
        public readonly IGridFunction<double> A, B;

        public Multiply(IGridFunction<double> a, IGridFunction<double> b)
        {
            A = a;
            B = b;
        }

        public double ValueAt(double x, double z)
        {
            return A.ValueAt(x, z) * B.ValueAt(x, z);
        }
    }
    
    public class ScaleWithBias : IGridFunction<double>
    {
        public readonly IGridFunction<double> Input;
        public readonly double Scale;
        public readonly double Bias;

        public ScaleWithBias(IGridFunction<double> input, double scale, double bias)
        {
            Input = input;
            Scale = scale;
            Bias = bias;
        }

        public double ValueAt(double x, double z)
        {
            return Input.ValueAt(x, z) * Scale + Bias;
        }
    }
    
    public class Min : IGridFunction<double>
    {
        public readonly IGridFunction<double> A, B;
        public readonly double Smoothness;

        public Min(IGridFunction<double> a, IGridFunction<double> b, double smoothness = 0)
        {
            A = a;
            B = b;
            Smoothness = smoothness;
        }

        public double ValueAt(double x, double z)
        {
            return Of(A.ValueAt(x, z), B.ValueAt(x, z), Smoothness);
        }
        
        public static double Of(double a, double b, double smoothness)
        {
            if (smoothness <= 0f) return Math.Min(a, b);
        
            double max = Math.Max(a, b) * smoothness;
            double min = Math.Min(a, b) * smoothness;
        
            return (min - Math.Log(1f + Math.Exp(min - max))) / smoothness;
        }
    }
    
    public class Max : IGridFunction<double>
    {
        public readonly IGridFunction<double> A, B;
        public readonly double Smoothness;

        public Max(IGridFunction<double> a, IGridFunction<double> b, double smoothness = 0)
        {
            A = a;
            B = b;
            Smoothness = smoothness;
        }

        public double ValueAt(double x, double z)
        {
            return Of(A.ValueAt(x, z), B.ValueAt(x, z), Smoothness);
        }
        
        public static double Of(double a, double b, double smoothness)
        {
            if (smoothness <= 0f) return Math.Max(a, b);
        
            double max = Math.Max(a, b) * smoothness;
            double min = Math.Min(a, b) * smoothness;
        
            return (max + Math.Log(1f + Math.Exp(min - max))) / smoothness;
        }
    }
    
    public class Clamp : IGridFunction<double>
    {
        public readonly IGridFunction<double> Input;
        public readonly double ClampMin;
        public readonly double ClampMax;

        public Clamp(IGridFunction<double> input, double clampMin, double clampMax)
        {
            Input = input;
            ClampMin = clampMin;
            ClampMax = clampMax;
        }

        public double ValueAt(double x, double z)
        {
            return Math.Max(ClampMin, Math.Min(ClampMax, Input.ValueAt(x, z)));
        }
    }
    
    public class SpanFunction : IGridFunction<double>
    {
        public readonly double Bias;
        public readonly double OriginX;
        public readonly double OriginZ;
        public readonly double SpanPx;
        public readonly double SpanNx;
        public readonly double SpanPz;
        public readonly double SpanNz;
        public readonly bool Circular;

        public SpanFunction(double bias, double originX, double originZ, double spanPx, double spanNx, 
            double spanPz, double spanNz, bool circular)
        {
            Bias = bias;
            OriginX = originX;
            OriginZ = originZ;
            SpanPx = spanPx;
            SpanNx = spanNx;
            SpanPz = spanPz;
            SpanNz = spanNz;
            Circular = circular;
        }

        public double ValueAt(double x, double z)
        {
            double diffX = x - OriginX;
            double diffZ = z - OriginZ;

            double spanX = diffX < 0 ? SpanNx : SpanPx;
            double spanZ = diffZ < 0 ? SpanNz : SpanPz;
        
            double valX = spanX == 0 ? 0 : Math.Abs(diffX) / spanX;
            double valZ = spanZ == 0 ? 0 : Math.Abs(diffZ) / spanZ;

            if (Circular)
            {
                return Bias + Math.Sqrt(Math.Pow(valX, 2) + Math.Pow(valZ, 2)) * (spanX < 0 || spanZ < 0 ? -1 : 1);
            }

            return Bias + valX + valZ;
        }
    }
    
    public class Rotate<T> : IGridFunction<T>
    {
        public readonly IGridFunction<T> Input;
        public readonly double PivotX;
        public readonly double PivotY;
        public readonly double Angle;

        public Rotate(IGridFunction<T> input, double pivotX, double pivotY, double angle)
        {
            Input = input;
            PivotX = pivotX;
            PivotY = pivotY;
            Angle = angle;
        }

        public T ValueAt(double x, double z)
        {
            double radians = DegToRad(Angle);
            
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
 
            x -= PivotX;
            z -= PivotY;
 
            double nx = x * cos - z * sin;
            double nz = x * sin + z * cos;
     
            nx += PivotX;
            nz += PivotY;
            
            return Input.ValueAt(nx, nz);
        }
    }
    
    public class Transform<T> : IGridFunction<T>
    {
        public readonly IGridFunction<T> Input;
        public readonly double TranslateX;
        public readonly double TranslateZ;
        public readonly double ScaleX;
        public readonly double ScaleZ;

        public Transform(IGridFunction<T> input, double translateX, double translateZ, double scaleX, double scaleZ)
        {
            Input = input;
            TranslateX = translateX;
            TranslateZ = translateZ;
            ScaleX = scaleX;
            ScaleZ = scaleZ;
        }

        public T ValueAt(double x, double z)
        {
            return Input.ValueAt(x * ScaleX + TranslateX, z * ScaleZ + TranslateZ);
        }
    }
    
    public static double DegToRad(double angle) {
        return (Math.PI / 180) * angle;
    }

    public delegate Func<double, double, double> NoiseFunction(double frequency, double lacunarity, double persistence, int octaves, int seed);

    public class NoiseGenerator : IGridFunction<double>
    {
        public readonly Func<double, double, double> Function;

        public NoiseGenerator(NoiseFunction function, double frequency, double lacunarity, double persistence, int octaves, int seed)
        {
            Function = function.Invoke(frequency, lacunarity, persistence, octaves, seed);
        }

        public double ValueAt(double x, double z)
        {
            return Function.Invoke(x, z);
        }
    }
}
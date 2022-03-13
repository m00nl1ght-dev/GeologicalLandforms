using System;

namespace TerrainGraph;

public abstract class GridFunction
{
    public static readonly Const Zero = new(0f);
    public static readonly Const One = new(1f);
    
    public abstract double ValueAt(double x, double y);

    public class Const : GridFunction
    {
        public readonly double Value;

        public Const(double value)
        {
            Value = value;
        }

        public override double ValueAt(double x, double y)
        {
            return Value;
        }
    }

    public class Add : GridFunction
    {
        public readonly GridFunction A, B;

        public Add(GridFunction a, GridFunction b)
        {
            A = a;
            B = b;
        }

        public override double ValueAt(double x, double y)
        {
            return A.ValueAt(x, y) + B.ValueAt(x, y);
        }
    }
    
    public class Multiply : GridFunction
    {
        public readonly GridFunction A, B;

        public Multiply(GridFunction a, GridFunction b)
        {
            A = a;
            B = b;
        }

        public override double ValueAt(double x, double y)
        {
            return A.ValueAt(x, y) * B.ValueAt(x, y);
        }
    }
    
    public class ScaleWithBias : GridFunction
    {
        public readonly GridFunction Input;
        public readonly double Scale;
        public readonly double Bias;

        public ScaleWithBias(GridFunction input, double scale, double bias)
        {
            Input = input;
            Scale = scale;
            Bias = bias;
        }

        public override double ValueAt(double x, double y)
        {
            return Input.ValueAt(x, y) * Scale + Bias;
        }
    }
    
    public class Min : GridFunction
    {
        public readonly GridFunction A, B;
        public readonly double Smoothness;

        public Min(GridFunction a, GridFunction b, double smoothness = 0)
        {
            A = a;
            B = b;
            Smoothness = smoothness;
        }

        public override double ValueAt(double x, double y)
        {
            return Of(A.ValueAt(x, y), B.ValueAt(x, y), Smoothness);
        }
        
        public static double Of(double a, double b, double smoothness)
        {
            if (smoothness <= 0f) return Math.Min(a, b);
        
            double max = Math.Max(a, b) * smoothness;
            double min = Math.Min(a, b) * smoothness;
        
            return (min - Math.Log(1f + Math.Exp(min - max))) / smoothness;
        }
    }
    
    public class Max : GridFunction
    {
        public readonly GridFunction A, B;
        public readonly double Smoothness;

        public Max(GridFunction a, GridFunction b, double smoothness = 0)
        {
            A = a;
            B = b;
            Smoothness = smoothness;
        }

        public override double ValueAt(double x, double y)
        {
            return Of(A.ValueAt(x, y), B.ValueAt(x, y), Smoothness);
        }
        
        public static double Of(double a, double b, double smoothness)
        {
            if (smoothness <= 0f) return Math.Max(a, b);
        
            double max = Math.Max(a, b) * smoothness;
            double min = Math.Min(a, b) * smoothness;
        
            return (max + Math.Log(1f + Math.Exp(min - max))) / smoothness;
        }
    }
    
    public class Rotate : GridFunction
    {
        public readonly GridFunction Input;
        public readonly double PivotX;
        public readonly double PivotY;
        public readonly double Angle;

        public Rotate(GridFunction input, double pivotX, double pivotY, double angle)
        {
            Input = input;
            PivotX = pivotX;
            PivotY = pivotY;
            Angle = angle;
        }

        public override double ValueAt(double x, double y)
        {
            double radians = DegToRad(Angle);
            
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
 
            x -= PivotX;
            y -= PivotY;
 
            double nx = x * cos - y * sin;
            double ny = x * sin + y * cos;
     
            nx += PivotX;
            ny += PivotY;
            
            return Input.ValueAt(nx, ny);
        }
    }
    
    public static double DegToRad(double angle) {
        return (Math.PI / 180) * angle;
    }

    public delegate Func<double, double, double> NoiseFunction(double frequency, double lacunarity, double persistence, int octaves, int seed);

    public class NoiseGenerator : GridFunction
    {
        public readonly Func<double, double, double> Function;

        public NoiseGenerator(NoiseFunction function, double frequency, double lacunarity, double persistence, int octaves, int seed)
        {
            Function = function.Invoke(frequency, lacunarity, persistence, octaves, seed);
        }

        public override double ValueAt(double x, double y)
        {
            return Function.Invoke(x, y);
        }
    }
}
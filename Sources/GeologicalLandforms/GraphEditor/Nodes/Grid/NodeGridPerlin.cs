using System;
using NodeEditorFramework;
using TerrainGraph;
using Verse.Noise;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Perlin Noise", 216)]
public class NodeGridPerlin : NodeGridNoise
{
    public const string ID = "gridPerlin";
    public override string GetID => ID;

    public override string Title => "Perlin Noise";
    
    protected override GridFunction.NoiseFunction NoiseFunction => PerlinNoise;

    private Func<double, double, double> PerlinNoise(double frequency, double lacunarity, double persistence, int octaves, int seed)
    {
        Perlin perlin = new(frequency, lacunarity, persistence, octaves, seed, QualityMode.High);
        return (x, y) => perlin.GetValue(x, 0, y);
    }
}
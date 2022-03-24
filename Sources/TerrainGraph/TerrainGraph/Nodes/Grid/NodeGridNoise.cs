using System;
using NodeEditorFramework;
using TerrainGraph.Util;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
public abstract class NodeGridNoise : NodeBase
{
    protected abstract GridFunction.NoiseFunction NoiseFunction { get; }
    
    [ValueConnectionKnob("Frequency", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob FrequencyKnob;
    
    [ValueConnectionKnob("Lacunarity", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob LacunarityKnob;
    
    [ValueConnectionKnob("Persistence", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob PersistenceKnob;
    
    [ValueConnectionKnob("Scale", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob ScaleKnob;
    
    [ValueConnectionKnob("Bias", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob BiasKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public double Frequency = 0.021;
    public double Lacunarity = 2;
    public double Persistence = 0.5;
    public double Scale = 0.5;
    public double Bias = 0.5;
    
    public int Octaves = 6;
    
    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        KnobValueField(FrequencyKnob, ref Frequency);
        KnobValueField(LacunarityKnob, ref Lacunarity);
        KnobValueField(PersistenceKnob, ref Persistence);
        KnobValueField(ScaleKnob, ref Scale);
        KnobValueField(BiasKnob, ref Bias);
        IntField("Octaves", ref Octaves);
        
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    public override void RefreshPreview()
    {
        ISupplier<double> freq = GetIfConnected<double>(FrequencyKnob);
        ISupplier<double> lac = GetIfConnected<double>(LacunarityKnob);
        ISupplier<double> pers = GetIfConnected<double>(PersistenceKnob);
        ISupplier<double> scale = GetIfConnected<double>(ScaleKnob);
        ISupplier<double> bias = GetIfConnected<double>(BiasKnob);
        
        foreach (ISupplier<double> supplier in new[]{freq, lac, pers, scale, bias}) supplier?.ResetState();
        
        if (freq != null) Frequency = freq.Get();
        if (lac != null) Lacunarity = lac.Get();
        if (pers != null) Persistence = pers.Get();
        if (scale != null) Scale = scale.Get();
        if (bias != null) Bias = bias.Get();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrValueFixed(FrequencyKnob, Frequency),
            SupplierOrValueFixed(LacunarityKnob, Lacunarity),
            SupplierOrValueFixed(PersistenceKnob, Persistence),
            SupplierOrValueFixed(ScaleKnob, Scale),
            SupplierOrValueFixed(BiasKnob, Bias),
            Octaves, NoiseFunction, CombinedSeed
        ));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly GridFunction.NoiseFunction _noiseFunction;
        private readonly ISupplier<double> _frequency;
        private readonly ISupplier<double> _lacunarity;
        private readonly ISupplier<double> _persistence;
        private readonly ISupplier<double> _scale;
        private readonly ISupplier<double> _bias;
        private readonly int _octaves;
        private readonly int _seed;
        private readonly FastRandom _random;

        public Output(ISupplier<double> frequency, ISupplier<double> lacunarity, 
            ISupplier<double> persistence, ISupplier<double> scale, ISupplier<double> bias, int octaves, GridFunction.NoiseFunction noiseFunction, int seed)
        {
            _noiseFunction = noiseFunction;
            _frequency = frequency;
            _lacunarity = lacunarity;
            _persistence = persistence;
            _scale = scale;
            _bias = bias;
            _octaves = octaves;
            _seed = seed;
            _random = new FastRandom(seed);
        }

        public IGridFunction<double> Get()
        {
            return new GridFunction.ScaleWithBias(
                new GridFunction.NoiseGenerator(
                    _noiseFunction, _frequency.Get(), _lacunarity.Get(), _persistence.Get(), _octaves, _random.Next()
                ), 
                _scale.Get(), _bias.Get()
            );
        }

        public void ResetState()
        {
            _random.Reinitialise(_seed);
            _frequency.ResetState();
            _lacunarity.ResetState();
            _persistence.ResetState();
            _scale.ResetState();
            _bias.ResetState();
        }
    }
}
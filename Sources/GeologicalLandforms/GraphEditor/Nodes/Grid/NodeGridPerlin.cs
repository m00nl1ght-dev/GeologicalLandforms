using System;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using TerrainGraph;
using TerrainGraph.Util;
using UnityEngine;
using Verse.Noise;

#pragma warning disable CS0659

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Perlin Noise", 216)]
public class NodeGridPerlin : NodeBase
{
    public const string ID = "gridPerlin";
    public override string GetID => ID;

    public override string Title => (DynamicSeed ? "Dynamic " : "") + "Perlin Noise";

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
    public bool DynamicSeed;

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

    public override void FillNodeActionsMenu(NodeEditorInputInfo inputInfo, GenericMenu menu)
    {
        base.FillNodeActionsMenu(inputInfo, menu);
        menu.AddSeparator("");

        if (!DynamicSeed)
        {
            menu.AddItem(new GUIContent("Enable dynamic seed"), false, () =>
            {
                DynamicSeed = true;
                canvas.OnNodeChange(this);
            });
        }
        else
        {
            menu.AddItem(new GUIContent("Disable dynamic seed"), false, () =>
            {
                DynamicSeed = false;
                canvas.OnNodeChange(this);
            });
        }
    }

    public override void RefreshPreview()
    {
        var freq = GetIfConnected<double>(FrequencyKnob);
        var lac = GetIfConnected<double>(LacunarityKnob);
        var pers = GetIfConnected<double>(PersistenceKnob);
        var scale = GetIfConnected<double>(ScaleKnob);
        var bias = GetIfConnected<double>(BiasKnob);

        foreach (var supplier in new[] { freq, lac, pers, scale, bias }) supplier?.ResetState();

        if (freq != null) Frequency = freq.Get();
        if (lac != null) Lacunarity = lac.Get();
        if (pers != null) Persistence = pers.Get();
        if (scale != null) Scale = scale.Get();
        if (bias != null) Bias = bias.Get();
    }

    public override void CleanUpGUI()
    {
        if (FrequencyKnob.connected()) Frequency = 0;
        if (LacunarityKnob.connected()) Lacunarity = 0;
        if (PersistenceKnob.connected()) Persistence = 0;
        if (ScaleKnob.connected()) Scale = 0;
        if (BiasKnob.connected()) Bias = 0;
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrFallback(FrequencyKnob, Frequency),
            SupplierOrFallback(LacunarityKnob, Lacunarity),
            SupplierOrFallback(PersistenceKnob, Persistence),
            SupplierOrFallback(ScaleKnob, Scale),
            SupplierOrFallback(BiasKnob, Bias),
            Octaves, Landform.MapSpaceToNodeSpaceFactor, CombinedSeed,
            DynamicSeed, TerrainCanvas.CreateRandomInstance()
        ));
        return true;
    }

    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<double> _frequency;
        private readonly ISupplier<double> _lacunarity;
        private readonly ISupplier<double> _persistence;
        private readonly ISupplier<double> _scale;
        private readonly ISupplier<double> _bias;
        private readonly int _octaves;
        private readonly double _transformScale;
        private readonly int _seed;
        private readonly bool _dynamicSeed;
        private readonly IRandom _random;

        public Output(
            ISupplier<double> frequency, ISupplier<double> lacunarity, ISupplier<double> persistence,
            ISupplier<double> scale, ISupplier<double> bias, int octaves,
            double transformScale, int seed, bool dynamicSeed, IRandom random)
        {
            _frequency = frequency;
            _lacunarity = lacunarity;
            _persistence = persistence;
            _scale = scale;
            _bias = bias;
            _octaves = octaves;
            _transformScale = transformScale;
            _seed = seed;
            _dynamicSeed = dynamicSeed;
            _random = random;
            _random.Reinitialise(_seed);
        }

        public IGridFunction<double> Get()
        {
            if (!_dynamicSeed) _random.Reinitialise(_seed);
            return new GridFunction.Transform<double>(
                new GridFunction.ScaleWithBias(
                    new NoiseFunction(
                        _frequency.Get(), _lacunarity.Get(), _persistence.Get(), _octaves, _random.Next()
                    ),
                    _scale.Get(), _bias.Get()), _transformScale
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

    public class NoiseFunction : IGridFunction<double>
    {
        public readonly double Frequency;
        public readonly double Lacunarity;
        public readonly double Persistence;
        public readonly int Octaves;
        public readonly int Seed;

        public NoiseFunction(double frequency, double lacunarity, double persistence, int octaves, int seed)
        {
            Frequency = frequency;
            Lacunarity = lacunarity;
            Persistence = persistence;
            Octaves = octaves;
            Seed = seed;
        }

        public double ValueAt(double x, double z)
        {
            return Perlin.GetValue(x, 0, z, Frequency, Seed, Lacunarity, Persistence, Octaves, QualityMode.High);
        }

        protected bool Equals(NoiseFunction other) =>
            Frequency.Equals(other.Frequency) &&
            Lacunarity.Equals(other.Lacunarity) &&
            Persistence.Equals(other.Persistence) &&
            Octaves == other.Octaves &&
            Seed == other.Seed;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NoiseFunction) obj);
        }

        public override string ToString() => $"PERLIN {{ freq {Frequency:F2} lac {Lacunarity:F2} pers {Persistence:F2} oct {Octaves} seed {Seed} }}";
    }
}

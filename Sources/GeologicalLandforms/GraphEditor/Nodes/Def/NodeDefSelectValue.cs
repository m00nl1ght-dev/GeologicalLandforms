using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using TerrainGraph.Util;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeDefSelectValue<T> : NodeSelectBase<string, double> where T : Def
{
    [ValueConnectionKnob("Output", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected abstract DefFunctionConnection<T> ConnectionType { get; }

    protected override string OptionConnectionTypeId => ValueFunctionConnection.Id;

    public override void RefreshPreview() => RefreshPreview(MathUtil.Identity);

    protected override void DrawOptionValue(int i) => DrawDoubleOptionValue(Values, i, true);

    protected override void DrawOptionKey(int i)
    {
        ConnectionType.SelectorUI(this, Thresholds[i], true, selected =>
        {
            Thresholds[i] = ConnectionType.ToString(selected);
            canvas.OnNodeChange(this);
        });
    }

    public override bool Calculate()
    {
        var input = SupplierOrFallback<T>(InputKnobRef, null);

        var keys = Thresholds.Select(ConnectionType.FromString).ToList();

        List<ISupplier<double>> options = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFallback(OptionKnobs[i], Values[i]));
        }

        OutputKnobRef.SetValue<ISupplier<double>>(new RevOutput(input, options, keys));
        return true;
    }

    protected class RevOutput : ISupplier<double>
    {
        private readonly ISupplier<T> _input;
        private readonly List<ISupplier<double>> _options;
        private readonly List<T> _keys;

        public RevOutput(ISupplier<T> input, List<ISupplier<double>> options, List<T> keys)
        {
            _input = input;
            _options = options;
            _keys = keys;
        }

        public double Get()
        {
            var value = _input.Get();

            for (int i = 1; i < _options.Count; i++)
            {
                if (value == _keys[i - 1]) return _options[i].Get();
            }

            return _options[0].Get();
        }

        public void ResetState()
        {
            _input.ResetState();
            foreach (var option in _options) option.ResetState();
        }
    }
}

[Serializable]
[Node(false, "Terrain/Select/Value", 309)]
public class NodeTerrainSelectValue : NodeDefSelectValue<TerrainDef>
{
    public const string ID = "terrainSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Biome/Select/Value", 339)]
public class NodeBiomeSelectValue : NodeDefSelectValue<BiomeDef>
{
    public const string ID = "biomeSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Roof/Select/Value", 329)]
public class NodeRoofSelectValue : NodeDefSelectValue<RoofDef>
{
    public const string ID = "roofSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, RoofFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

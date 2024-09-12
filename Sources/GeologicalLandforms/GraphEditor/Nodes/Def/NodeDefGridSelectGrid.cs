using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeDefGridSelectGrid<T> : NodeSelectBase<string, double> where T : Def
{
    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected abstract DefFunctionConnection<T> ConnectionType { get; }

    protected override string OptionConnectionTypeId => GridFunctionConnection.Id;

    protected override void DrawOptionValue(int i) => DrawDoubleOptionValue(Values, i);

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
        var input = SupplierOrFallback(InputKnobRef, GridFunction.Of(default(T)));

        var keys = Thresholds.Select(ConnectionType.FromString).ToList();

        List<ISupplier<IGridFunction<double>>> options = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFallback(OptionKnobs[i], GridFunction.Of(Values[i])));
        }

        OutputKnobRef.SetValue<ISupplier<IGridFunction<double>>>(new RevOutput(input, options, keys));
        return true;
    }

    protected class RevOutput : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<IGridFunction<T>> _input;
        private readonly List<ISupplier<IGridFunction<double>>> _options;
        private readonly List<T> _keys;

        public RevOutput(ISupplier<IGridFunction<T>> input, List<ISupplier<IGridFunction<double>>> options, List<T> keys)
        {
            _input = input;
            _options = options;
            _keys = keys;
        }

        public IGridFunction<double> Get()
        {
            return new GridFunction.SelectDiscrete<T, double>(
                _input.Get(), _options.Skip(1).Select(o => o.Get()).ToList(), _keys, _options.First().Get()
            );
        }

        public void ResetState()
        {
            _input.ResetState();
            foreach (var option in _options) option.ResetState();
        }
    }
}

[Serializable]
[Node(false, "Terrain/Grid/Select/Value", 305)]
public class NodeTerrainGridSelectValue : NodeDefGridSelectGrid<TerrainDef>
{
    public const string ID = "terrainGridSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Biome/Grid/Select/Value", 306)]
public class NodeBiomeGridSelectValue : NodeDefGridSelectGrid<BiomeDef>
{
    public const string ID = "biomeGridSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Roof/Grid/Select/Value", 307)]
public class NodeRoofGridSelectValue : NodeDefGridSelectGrid<RoofDef>
{
    public const string ID = "roofGridSelectValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

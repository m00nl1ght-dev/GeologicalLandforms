using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using TerrainGraph.Util;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Operator")]
public class NodeGridOperator : NodeOperatorBase
{
    public const string ID = "gridOperator";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;
    
    protected override void CreateNewInputKnob()
    {
        CreateValueConnectionKnob(new("Input " + InputKnobs.Count, Direction.In, GridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override bool Calculate()
    {
        ISupplier<double> applyChance = SupplierOrFixed(ApplyChanceKnob, ApplyChance);
        ISupplier<double> smoothness = SupplierOrFixed(SmoothnessKnob, Smoothness);
        
        List<ISupplier<GridFunction>> inputs = InputKnobs
            .Select(knob => knob != null && knob.connected() ? knob.GetValue<ISupplier<GridFunction>>() : null)
            .Where(f => f != null).ToList();

        OutputKnob.SetValue<ISupplier<GridFunction>>(new Output(applyChance, inputs, OperationType, smoothness, CombinedSeed));
        return true;
    }
    
    private class Output : ISupplier<GridFunction>
    {
        private readonly ISupplier<double> _applyChance;
        private readonly List<ISupplier<GridFunction>> _inputs;
        private readonly FastRandom _random;
        private readonly Operation _operationType;
        private readonly ISupplier<double> _smoothness;
        private readonly int _seed;

        public Output(ISupplier<double> applyChance, List<ISupplier<GridFunction>> inputs, Operation operationType, ISupplier<double> smoothness, int seed)
        {
            _applyChance = applyChance;
            _inputs = inputs;
            _operationType = operationType;
            _smoothness = smoothness;
            _seed = seed;
            _random = new FastRandom(seed);
        }

        public GridFunction Get()
        {
            double applyChance = _applyChance.Get();
            double smoothness = _smoothness.Get();
            List<GridFunction> inputs = _inputs
                .Where((_, i) => i == 0 || applyChance >= 1 || _random.NextDouble() > applyChance)
                .Select(e => e.Get()).ToList();
            
            return inputs.Count switch
            {
                0 => GridFunction.Zero,
                1 => inputs[0],
                _ => _operationType switch
                {
                    Operation.Add => inputs.Aggregate((a, b) => new GridFunction.Add(a, b)),
                    Operation.Multiply => inputs.Aggregate((a, b) => new GridFunction.Multiply(a, b)),
                    Operation.Min or Operation.Smooth_Min => inputs.Aggregate((a, b) => new GridFunction.Min(a, b, smoothness)),
                    Operation.Max or Operation.Smooth_Max => inputs.Aggregate((a, b) => new GridFunction.Max(a, b, smoothness)),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        public void ResetState()
        {
            _random.Reinitialise(_seed);
            foreach (ISupplier<GridFunction> input in _inputs) input.ResetState();
            _applyChance.ResetState();
            _smoothness.ResetState();
        }
    }
}
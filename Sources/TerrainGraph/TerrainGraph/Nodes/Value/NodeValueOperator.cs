using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using TerrainGraph.Util;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Operator", 110)]
public class NodeValueOperator : NodeOperatorBase
{
    public const string ID = "valueOperator";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public List<double> Values = new();

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
        while (InputKnobs.Count < 2) CreateNewInputKnob();
        
        GUILayout.BeginVertical(BoxStyle);

        if (SmoothnessKnob != null) KnobValueField(SmoothnessKnob, ref Smoothness);
        if (ApplyChanceKnob != null) KnobValueField(ApplyChanceKnob, ref ApplyChance);

        while (Values.Count < InputKnobs.Count) Values.Add(0f);
        while (Values.Count > InputKnobs.Count) Values.RemoveAt(Values.Count - 1);
        
        for (int i = 0; i < InputKnobs.Count; i++)
        {
            ValueConnectionKnob knob = InputKnobs[i];
            var value = Values[i];
            KnobValueField(knob, ref value, i == 0 ? "Base" : "Input " + i);
            Values[i] = value;
        }

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    protected override void CreateNewInputKnob()
    {
        CreateValueConnectionKnob(new("Input " + InputKnobs.Count, Direction.In, ValueFunctionConnection.Id));
        RefreshDynamicKnobs();
    }
    
    public override void RefreshPreview()
    {
        base.RefreshPreview();
        List<ISupplier<double>> suppliers = new();

        for (int i = 0; i < Math.Min(Values.Count, InputKnobs.Count); i++)
        {
            ValueConnectionKnob knob = InputKnobs[i];
            ISupplier<double> supplier = GetIfConnected<double>(knob);
            supplier?.ResetState();
            suppliers.Add(supplier);
        }

        for (var i = 0; i < suppliers.Count; i++)
        {
            if (suppliers[i] != null) Values[i] = suppliers[i].Get();
        }
    }

    public override bool Calculate()
    {
        ISupplier<double> applyChance = SupplierOrValueFixed(ApplyChanceKnob, ApplyChance);
        ISupplier<double> smoothness = SupplierOrValueFixed(SmoothnessKnob, Smoothness);
        
        List<ISupplier<double>> inputs = new();
        for (int i = 0; i < Math.Min(Values.Count, InputKnobs.Count); i++)
        {
            inputs.Add(SupplierOrValueFixed(InputKnobs[i], Values[i]));
        }
        
        OutputKnob.SetValue<ISupplier<double>>(new Output(applyChance, inputs, OperationType, smoothness, CombinedSeed));
        return true;
    }

    private class Output : ISupplier<double>
    {
        private readonly ISupplier<double> _applyChance;
        private readonly List<ISupplier<double>> _inputs;
        private readonly FastRandom _random;
        private readonly Operation _operationType;
        private readonly ISupplier<double> _smoothness;
        private readonly int _seed;

        public Output(ISupplier<double> applyChance, List<ISupplier<double>> inputs, Operation operationType, ISupplier<double> smoothness, int seed)
        {
            _applyChance = applyChance;
            _inputs = inputs;
            _operationType = operationType;
            _smoothness = smoothness;
            _seed = seed;
            _random = new FastRandom(seed);
        }

        public double Get()
        {
            double applyChance = _applyChance.Get();
            double smoothness = _smoothness.Get();
            List<double> inputs = _inputs
                .Where((e, i) => i == 0 || applyChance >= 1 || _random.NextDouble() < applyChance)
                .Select(e => e.Get()).ToList();
            
            return inputs.Count switch
            {
                0 => 0f,
                1 => inputs[0],
                _ => _operationType switch
                {
                    Operation.Add => inputs.Aggregate((a, b) => a + b),
                    Operation.Multiply => inputs.Aggregate((a, b) => a * b),
                    Operation.Min or Operation.Smooth_Min => inputs.Aggregate((a, b) => GridFunction.Min.Of(a, b, smoothness)),
                    Operation.Max or Operation.Smooth_Max => inputs.Aggregate((a, b) => GridFunction.Max.Of(a, b, smoothness)),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        public void ResetState()
        {
            _random.Reinitialise(_seed);
            foreach (ISupplier<double> input in _inputs) input.ResetState();
            _applyChance.ResetState();
            _smoothness.ResetState();
        }
    }
}
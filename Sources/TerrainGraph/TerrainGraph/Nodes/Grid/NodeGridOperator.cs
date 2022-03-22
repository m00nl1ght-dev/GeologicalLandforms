using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using TerrainGraph.Util;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Operator", 211)]
public class NodeGridOperator : NodeOperatorBase
{
    public const string ID = "gridOperator";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
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
            
            GUILayout.BeginHorizontal(BoxStyle);
            GUILayout.Label(i == 0 ? "Base" : ("Input " + i), BoxLayout);

            if (!knob.connected())
            {
                GUILayout.FlexibleSpace();
                Values[i] = RTEditorGUI.FloatField(GUIContent.none, (float) Values[i], BoxLayout);
            }

            GUILayout.EndHorizontal();
            knob.SetPosition();
        }

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    protected override void CreateNewInputKnob()
    {
        CreateValueConnectionKnob(new("Input " + InputKnobs.Count, Direction.In, GridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override bool Calculate()
    {
        ISupplier<double> applyChance = SupplierOrValueFixed(ApplyChanceKnob, ApplyChance);
        ISupplier<double> smoothness = SupplierOrValueFixed(SmoothnessKnob, Smoothness);
        
        List<ISupplier<IGridFunction<double>>> inputs = new();
        for (int i = 0; i < Math.Min(Values.Count, InputKnobs.Count); i++)
        {
            inputs.Add(SupplierOrGridFixed(InputKnobs[i], GridFunction.Of(Values[i])));
        }

        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(applyChance, inputs, OperationType, smoothness, CombinedSeed));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<double> _applyChance;
        private readonly List<ISupplier<IGridFunction<double>>> _inputs;
        private readonly FastRandom _random;
        private readonly Operation _operationType;
        private readonly ISupplier<double> _smoothness;
        private readonly int _seed;

        public Output(ISupplier<double> applyChance, List<ISupplier<IGridFunction<double>>> inputs, Operation operationType, ISupplier<double> smoothness, int seed)
        {
            _applyChance = applyChance;
            _inputs = inputs;
            _operationType = operationType;
            _smoothness = smoothness;
            _seed = seed;
            _random = new FastRandom(seed);
        }

        public IGridFunction<double> Get()
        {
            double applyChance = _applyChance.Get();
            double smoothness = _smoothness.Get();
            List<IGridFunction<double>> inputs = _inputs
                .Where((_, i) => i == 0 || applyChance >= 1 || _random.NextDouble() < applyChance)
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
            foreach (ISupplier<IGridFunction<double>> input in _inputs) input.ResetState();
            _applyChance.ResetState();
            _smoothness.ResetState();
        }
    }
}
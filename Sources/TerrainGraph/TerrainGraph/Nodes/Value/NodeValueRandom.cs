using System;
using NodeEditorFramework;
using TerrainGraph.Util;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Random", 111)]
public class NodeValueRandom : NodeBase
{
    public const string ID = "valueRandom";
    public override string GetID => ID;

    public override string Title => "Random Value";
    
    [ValueConnectionKnob("Average", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob AverageKnob;
    
    [ValueConnectionKnob("Deviation", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob DeviationKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ValueFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public double Average = 0.5;
    public double Deviation = 0.5;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        KnobValueField(AverageKnob, ref Average);
        KnobValueField(DeviationKnob, ref Deviation);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    public override void RefreshPreview()
    {
        ISupplier<double> avg = GetIfConnected<double>(AverageKnob);
        ISupplier<double> dev = GetIfConnected<double>(DeviationKnob);
        
        avg?.ResetState();
        dev?.ResetState();

        if (avg != null) Average = avg.Get();
        if (dev != null) Deviation = dev.Get();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<double>>(new Output(
            SupplierOrValueFixed(AverageKnob, Average), 
            SupplierOrValueFixed(DeviationKnob, Deviation), 
            CombinedSeed
        ));
        return true;
    }
    
    private class Output : ISupplier<double>
    {
        private readonly FastRandom _random;
        private readonly ISupplier<double> _average;
        private readonly ISupplier<double> _deviation;
        private readonly int _seed;

        public Output(ISupplier<double> average, ISupplier<double> deviation, int seed)
        {
            _average = average;
            _deviation = deviation;
            _seed = seed;
            _random = new FastRandom(seed);
        }

        public double Get()
        {
            double average = _average.Get();
            double deviation = _deviation.Get();
            return _random.NextDouble(average - deviation, average + deviation);
        }

        public void ResetState()
        {
            _random.Reinitialise(_seed);
            _average.ResetState();
            _deviation.ResetState();
        }
    }
}
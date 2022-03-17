using System;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Select/Grid", 101)]
public class NodeValueSelectGridValue : NodeSelectBase
{
    public const string ID = "valueSelectGridValue";
    public override string GetID => ID;
    
    [ValueConnectionKnob("Input", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;
    
    public List<double> Values = new();

    public override void NodeGUI()
    {
        while (OptionKnobs.Count < 2) CreateNewOptionKnob();
        
        while (Values.Count < OptionKnobs.Count) Values.Add(0f);
        while (Values.Count > OptionKnobs.Count) Values.RemoveAt(Values.Count - 1);
        
        base.NodeGUI();
    }

    protected override void DrawOption(ValueConnectionKnob knob, int i)
    {
        if (knob.connected())
        {
            GUILayout.Label("Option " + (i + 1), BoxLayout);
        }
        else
        {
            Values[i] = RTEditorGUI.FloatField(GUIContent.none, (float) Values[i], BoxLayout);
        }
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, GridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override bool Calculate()
    {
        ISupplier<double> input = SupplierOrValueFixed(InputKnob, 0d);

        List<ISupplier<IGridFunction<double>>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrGridFixed(OptionKnobs[i], GridFunction.Of(Values[i])));
        }
        
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output<IGridFunction<double>>(input, options, Thresholds));
        return true;
    }
}
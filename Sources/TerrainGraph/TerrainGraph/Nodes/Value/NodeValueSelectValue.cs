using System;
using System.Collections.Generic;
using System.Globalization;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Select/Value", 100)]
public class NodeValueSelectValue : NodeSelectBase
{
    public const string ID = "valueSelectValue";
    public override string GetID => ID;
    
    [ValueConnectionKnob("Input", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, ValueFunctionConnection.Id)]
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
            GUILayout.Label(Math.Round(Values[i], 2).ToString(CultureInfo.InvariantCulture), BoxLayout);
        }
        else
        {
            Values[i] = RTEditorGUI.FloatField(GUIContent.none, (float) Values[i], BoxLayout);
        }
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, ValueFunctionConnection.Id));
        RefreshDynamicKnobs();
    }
    
    public override void RefreshPreview()
    {
        base.RefreshPreview();
        List<ISupplier<double>> suppliers = new();
        
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            ValueConnectionKnob knob = OptionKnobs[i];
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
        ISupplier<double> input = SupplierOrValueFixed(InputKnob, 0d);

        List<ISupplier<double>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrValueFixed(OptionKnobs[i], Values[i]));
        }
        
        OutputKnob.SetValue<ISupplier<double>>(new Output<double>(input, options, Thresholds));
        return true;
    }
}
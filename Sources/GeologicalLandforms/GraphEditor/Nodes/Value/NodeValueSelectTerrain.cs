using System;
using System.Collections.Generic;
using NodeEditorFramework;
using Verse;

namespace TerrainGraph;

[Serializable]
[Node(false, "Value/Select/Terrain")]
public class NodeValueSelectTerrain : NodeSelectBase
{
    public const string ID = "valueSelectTerrain";
    public override string GetID => ID;
    
    [ValueConnectionKnob("Input", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;
    
    public List<string> Values = new();

    public override void NodeGUI()
    {
        while (OptionKnobs.Count < 2) CreateNewOptionKnob();
        
        while (Values.Count < OptionKnobs.Count) Values.Add("");
        while (Values.Count > OptionKnobs.Count) Values.RemoveAt(Values.Count - 1);
        
        base.NodeGUI();
    }

    protected override void DrawOption(ValueConnectionKnob knob, int i)
    {
        TerrainFunctionConnection.TerrainSelector(this, Values[i], !knob.connected(), selected =>
        {
            Values[i] = TerrainFunctionConnection.ToString(selected);
            canvas.OnNodeChange(this);
        });
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, TerrainFunctionConnection.Id));
        RefreshDynamicKnobs();
    }
    
    public override void RefreshPreview()
    {
        base.RefreshPreview();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            ValueConnectionKnob knob = OptionKnobs[i];
            Values[i] = TerrainFunctionConnection.ToString(RefreshIfConnected(knob, TerrainFunctionConnection.FromString(Values[i])));
        }
    }
    
    public override bool Calculate()
    {
        ISupplier<double> input = SupplierOrValueFixed(InputKnob, 0d);

        List<ISupplier<TerrainDef>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFixed(OptionKnobs[i], TerrainFunctionConnection.FromString(Values[i])));
        }
        
        OutputKnob.SetValue<ISupplier<TerrainDef>>(new Output<TerrainDef>(input, options, Thresholds));
        return true;
    }
}
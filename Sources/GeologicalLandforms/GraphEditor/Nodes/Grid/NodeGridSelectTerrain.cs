using System;
using System.Collections.Generic;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Select/Terrain")]
public class NodeGridSelectTerrain : NodeSelectBase
{
    public const string ID = "gridSelectTerrain";
    public override string GetID => ID;
    
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
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
        if (knob.connected())
        {
            GUILayout.Label("Option " + (i + 1), BoxLayout);
        }
        else
        {
            TerrainFunctionConnection.TerrainSelector(this, Values[i], true, selected =>
            {
                Values[i] = TerrainFunctionConnection.ToString(selected);
                canvas.OnNodeChange(this);
            });
        }
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, TerrainGridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override bool Calculate()
    {
        ISupplier<IGridFunction<double>> input = SupplierOrGridFixed(InputKnob, GridFunction.Zero);

        List<ISupplier<IGridFunction<TerrainDef>>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFixed(OptionKnobs[i], GridFunction.Of(TerrainFunctionConnection.FromString(Values[i]))));
        }
        
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainDef>>>(new GridOutput<TerrainDef>(input, options, Thresholds));
        return true;
    }
}
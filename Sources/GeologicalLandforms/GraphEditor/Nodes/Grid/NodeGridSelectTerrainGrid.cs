using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Select/Terrain Grid", 202)]
public class NodeGridSelectTerrainGrid : NodeSelectBase
{
    public const string ID = "gridSelectTerrainGrid";
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
            TerrainData.TerrainSelector(this, Values[i], true, selected =>
            {
                Values[i] = TerrainData.ToString(selected);
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

        List<ISupplier<IGridFunction<TerrainData>>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrGridFixed(OptionKnobs[i], GridFunction.Of(TerrainData.FromString(Values[i]))));
        }

        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainData>>>(new GridOutput<TerrainData>(input, options, Thresholds, PostProcess));
        return true;
    }

    private TerrainData PostProcess(TerrainData result, int index)
    {
        return new TerrainData(result.Terrain, index);
    }
}

using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Select/Biome Grid", 204)]
public class NodeGridSelectBiomeGrid : NodeSelectBase
{
    public const string ID = "gridSelectBiomeGrid";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public List<string> Values = [];

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
            BiomeData.BiomeSelector(this, Values[i], true, selected =>
            {
                Values[i] = BiomeData.ToString(selected);
                canvas.OnNodeChange(this);
            });
        }
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, BiomeGridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override bool Calculate()
    {
        ISupplier<IGridFunction<double>> input = SupplierOrFallback(InputKnob, GridFunction.Zero);

        List<ISupplier<IGridFunction<BiomeData>>> options = [];
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFallback(OptionKnobs[i], GridFunction.Of(BiomeData.FromString(Values[i]))));
        }

        OutputKnob.SetValue<ISupplier<IGridFunction<BiomeData>>>(new GridOutput<BiomeData>(input, options, Thresholds, PostProcess));
        return true;
    }

    private BiomeData PostProcess(BiomeData result, int index)
    {
        return new BiomeData(result.Biome, index);
    }
}

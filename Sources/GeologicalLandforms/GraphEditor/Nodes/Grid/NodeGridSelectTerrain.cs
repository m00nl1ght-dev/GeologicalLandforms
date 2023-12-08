using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Select/Terrain", 201)]
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
        TerrainData.TerrainSelector(this, Values[i], !knob.connected(), selected =>
        {
            Values[i] = TerrainData.ToString(selected);
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
        List<ISupplier<TerrainData>> suppliers = new();

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            ValueConnectionKnob knob = OptionKnobs[i];
            ISupplier<TerrainData> supplier = GetIfConnected<TerrainData>(knob);
            supplier?.ResetState();
            suppliers.Add(supplier);
        }

        for (var i = 0; i < suppliers.Count; i++)
        {
            if (suppliers[i] != null) Values[i] = suppliers[i].Get().ToString();
        }
    }

    public override bool Calculate()
    {
        var input = SupplierOrFallback(InputKnob, GridFunction.Zero);

        List<ISupplier<IGridFunction<TerrainData>>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(new NodeGridFromValue.Output<TerrainData>(SupplierOrFallback(OptionKnobs[i], TerrainData.FromString(Values[i]))));
        }

        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainData>>>(new GridOutput<TerrainData>(input, options, Thresholds, PostProcess));
        return true;
    }

    private TerrainData PostProcess(TerrainData result, int index)
    {
        return new TerrainData(result.Terrain, index);
    }
}

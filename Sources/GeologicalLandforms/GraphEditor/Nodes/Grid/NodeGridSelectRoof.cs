using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Select/Roof", 205)]
public class NodeGridSelectRoof : NodeSelectBase
{
    public const string ID = "gridSelectRoof";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
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
        RoofData.RoofSelector(this, Values[i], !knob.connected(), selected =>
        {
            Values[i] = RoofData.ToString(selected);
            canvas.OnNodeChange(this);
        });
    }

    protected override void CreateNewOptionKnob()
    {
        CreateValueConnectionKnob(new("Option " + OptionKnobs.Count, Direction.In, RoofFunctionConnection.Id));
        RefreshDynamicKnobs();
        canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        base.RefreshPreview();
        List<ISupplier<RoofData>> suppliers = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            var knob = OptionKnobs[i];
            var supplier = GetIfConnected<RoofData>(knob);
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

        List<ISupplier<IGridFunction<RoofData>>> options = [];
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(new NodeGridFromValue.Output<RoofData>(SupplierOrFallback(OptionKnobs[i], RoofData.FromString(Values[i]))));
        }

        OutputKnob.SetValue<ISupplier<IGridFunction<RoofData>>>(new GridOutput<RoofData>(input, options, Thresholds, null, PostProcess));
        return true;
    }

    private RoofData PostProcess(RoofData result, int index)
    {
        return new RoofData(result.Roof, index);
    }
}

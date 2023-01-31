using System;
using System.Collections.Generic;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Value/Select/Terrain", 103)]
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
        ISupplier<double> input = SupplierOrValueFixed(InputKnob, 0d);

        List<ISupplier<TerrainData>> options = new();
        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFixed(OptionKnobs[i], TerrainData.FromString(Values[i])));
        }

        OutputKnob.SetValue<ISupplier<TerrainData>>(new Output<TerrainData>(input, options, Thresholds));
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeValueSelectDef<T> : NodeSelectBase<double, string> where T : Def
{
    [ValueConnectionKnob("Input", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected abstract DefFunctionConnection<T> ConnectionType { get; }

    protected override string OptionConnectionTypeId => ConnectionType.Identifier;

    public override void RefreshPreview() => RefreshPreview<T>(ConnectionType.ToString);

    protected override void DrawOptionKey(int i) => DrawDoubleOptionKey(Thresholds, i);

    protected override void DrawOptionValue(int i)
    {
        ConnectionType.SelectorUI(this, Values[i], !OptionKnobs[i].connected(), selected =>
        {
            Values[i] = ConnectionType.ToString(selected);
            canvas.OnNodeChange(this);
        });
    }

    public override bool Calculate()
    {
        var input = SupplierOrFallback(InputKnobRef, 0d);

        List<ISupplier<T>> options = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            options.Add(SupplierOrFallback(OptionKnobs[i], ConnectionType.FromString(Values[i])));
        }

        OutputKnobRef.SetValue<ISupplier<T>>(new Output<T>(input, options, Thresholds.ToList(), null));
        return true;
    }
}

[Serializable]
[Node(false, "Value/Select/Terrain", 103)]
public class NodeValueSelectTerrain : NodeValueSelectDef<TerrainDef>
{
    public const string ID = "valueSelectTerrain";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Value/Select/Biome", 104)]
public class NodeValueSelectBiome : NodeValueSelectDef<BiomeDef>
{
    public const string ID = "valueSelectBiome";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Value/Select/Roof", 105)]
public class NodeValueSelectRoof : NodeValueSelectDef<RoofDef>
{
    public const string ID = "valueSelectRoof";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, RoofFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

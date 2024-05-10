using System;
using System.Collections.Generic;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeGridSelectDef<T> : NodeSelectBase<double, string> where T : Def
{
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
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
        var input = SupplierOrFallback(InputKnob, GridFunction.Zero);

        List<ISupplier<IGridFunction<T>>> options = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            var value = ConnectionType.FromString(Values[i]);
            options.Add(new NodeGridFromValue.Output<T>(SupplierOrFallback(OptionKnobs[i], value)));
        }

        OutputKnobRef.SetValue<ISupplier<IGridFunction<T>>>(new GridOutput<T>(input, options, Thresholds, null));
        return true;
    }
}

[Serializable]
[Node(false, "Grid/Select/Terrain", 201)]
public class NodeGridSelectTerrain : NodeGridSelectDef<TerrainDef>
{
    public const string ID = "gridSelectTerrain";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Grid/Select/Biome", 202)]
public class NodeGridSelectBiome : NodeGridSelectDef<BiomeDef>
{
    public const string ID = "gridSelectBiome";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Grid/Select/Roof", 203)]
public class NodeGridSelectRoof : NodeGridSelectDef<RoofDef>
{
    public const string ID = "gridSelectRoof";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

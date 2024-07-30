using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeGridSelectDefGrid<T> : NodeSelectBase<double, string> where T : Def
{
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;

    protected abstract DefFunctionConnection<T> ConnectionType { get; }

    protected override void DrawOptionKey(int i) => DrawDoubleOptionKey(Thresholds, i);

    protected override void DrawOptionValue(int i)
    {
        if (OptionKnobs[i].connected())
        {
            GUILayout.Label("Option " + (i + 1), BoxLayout);
        }
        else
        {
            ConnectionType.SelectorUI(this, Values[i], true, selected =>
            {
                Values[i] = ConnectionType.ToString(selected);
                canvas.OnNodeChange(this);
            });
        }
    }

    public override bool Calculate()
    {
        var input = SupplierOrFallback(InputKnob, GridFunction.Zero);

        List<ISupplier<IGridFunction<T>>> options = [];

        for (int i = 0; i < Math.Min(Values.Count, OptionKnobs.Count); i++)
        {
            var value = ConnectionType.FromString(Values[i]);
            options.Add(SupplierOrFallback(OptionKnobs[i], GridFunction.Of(value)));
        }

        OutputKnobRef.SetValue<ISupplier<IGridFunction<T>>>(new GridOutput<T>(input, options, Thresholds.ToList(), null));
        return true;
    }
}

[Serializable]
[Node(false, "Grid/Select/Terrain Grid", 205)]
public class NodeGridSelectTerrainGrid : NodeGridSelectDefGrid<TerrainDef>
{
    public const string ID = "gridSelectTerrainGrid";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override string OptionConnectionTypeId => TerrainGridFunctionConnection.Id;

    protected override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Grid/Select/Biome Grid", 206)]
public class NodeGridSelectBiomeGrid : NodeGridSelectDefGrid<BiomeDef>
{
    public const string ID = "gridSelectBiomeGrid";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override string OptionConnectionTypeId => BiomeGridFunctionConnection.Id;

    protected override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Grid/Select/Roof Grid", 207)]
public class NodeGridSelectRoofGrid : NodeGridSelectDefGrid<RoofDef>
{
    public const string ID = "gridSelectRoofGrid";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    protected override string OptionConnectionTypeId => RoofGridFunctionConnection.Id;

    protected override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

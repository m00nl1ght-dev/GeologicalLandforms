using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeDefGridFromValue<T> : NodeBase where T : Def
{
    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Grid";

    public abstract ValueConnectionKnob InputKnobRef { get; }
    public abstract ValueConnectionKnob OutputKnobRef { get; }

    public abstract DefFunctionConnection<T> ConnectionType { get; }

    public string Value;

    public override void NodeGUI()
    {
        InputKnobRef.SetPosition(FirstKnobPosition);
        OutputKnobRef.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        ConnectionType.SelectorUI(this, Value, !InputKnobRef.connected(), selected =>
        {
            Value = ConnectionType.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        var input = GetIfConnected<T>(InputKnobRef);

        input?.ResetState();

        if (input != null) Value = ConnectionType.ToString(input.Get());
    }

    public override void CleanUpGUI()
    {
        if (InputKnobRef.connected()) Value = null;
    }

    public override bool Calculate()
    {
        OutputKnobRef.SetValue<ISupplier<IGridFunction<T>>>(new NodeGridFromValue.Output<T>(
            SupplierOrFallback(InputKnobRef, ConnectionType.FromString(Value))
        ));
        return true;
    }
}

[Serializable]
[Node(false, "Terrain/Grid/Const", 300)]
public class NodeTerrainGridFromValue : NodeDefGridFromValue<TerrainDef>
{
    public const string ID = "terrainGridFromValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Biome/Grid/Const", 338)]
public class NodeBiomeGridFromValue : NodeDefGridFromValue<BiomeDef>
{
    public const string ID = "biomeGridFromValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Roof/Grid/Const", 328)]
public class NodeRoofGridFromValue : NodeDefGridFromValue<RoofDef>
{
    public const string ID = "roofGridFromValue";
    public override string GetID => ID;

    [ValueConnectionKnob("Input", Direction.In, RoofFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, RoofGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace TerrainGraph;

public abstract class NodeDefConst<T> : NodeBase where T : Def
{
    public override Vector2 DefaultSize => new(100, 55);
    public override bool AutoLayout => false;

    public override string Title => "Const";

    public abstract ValueConnectionKnob OutputKnobRef { get; }

    public abstract DefFunctionConnection<T> ConnectionType { get; }

    public string Value;

    public override void NodeGUI()
    {
        OutputKnobRef.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        ConnectionType.SelectorUI(this, Value, true, selected =>
        {
            Value = ConnectionType.ToString(selected);
            canvas.OnNodeChange(this);
        }, FullBoxLayout);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnobRef.SetValue<ISupplier<T>>(Supplier.Of(ConnectionType.FromString(Value)));
        return true;
    }
}

[Serializable]
[Node(false, "Terrain/Const", 310)]
public class NodeTerrainConst : NodeDefConst<TerrainDef>
{
    public const string ID = "terrainConst";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<TerrainDef> ConnectionType => TerrainFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Biome/Const", 340)]
public class NodeBiomeConst : NodeDefConst<BiomeDef>
{
    public const string ID = "biomeConst";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<BiomeDef> ConnectionType => BiomeFunctionConnection.Instance;
}

[Serializable]
[Node(false, "Roof/Const", 330)]
public class NodeRoofConst : NodeDefConst<RoofDef>
{
    public const string ID = "roofConst";
    public override string GetID => ID;

    [ValueConnectionKnob("Output", Direction.Out, RoofFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    public override DefFunctionConnection<RoofDef> ConnectionType => RoofFunctionConnection.Instance;
}

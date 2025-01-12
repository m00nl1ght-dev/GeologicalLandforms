using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Terrain Patches", 403)]
public class NodeOutputTerrainPatches : NodeOutputBase
{
    public const string ID = "outputTerrainPatches";
    public override string GetID => ID;

    public override string Title => "Terrain Patches";

    public override ValueConnectionKnob InputKnobRef => OffsetKnob;

    [ValueConnectionKnob("Offset", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob OffsetKnob;

    [ValueConnectionKnob("Frequency", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob FrequencyKnob;

    [ValueConnectionKnob("Lacunarity", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob LacunarityKnob;

    [ValueConnectionKnob("Persistence", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob PersistenceKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Value offset", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        OffsetKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Frequency multiplier", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        FrequencyKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Lacunarity multiplier", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        LacunarityKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Persistence multiplier", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        PersistenceKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputTerrainPatches;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputTerrainPatches = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputTerrainPatches == this) Landform.OutputTerrainPatches = null;
    }

    public IGridFunction<double> GetOffset()
    {
        return OffsetKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }

    public double? GetFrequencyFactor()
    {
        return FrequencyKnob.GetValue<ISupplier<double>>()?.ResetAndGet();
    }

    public double? GetLacunarityFactor()
    {
        return LacunarityKnob.GetValue<ISupplier<double>>()?.ResetAndGet();
    }

    public double? GetPersistenceFactor()
    {
        return PersistenceKnob.GetValue<ISupplier<double>>()?.ResetAndGet();
    }
}

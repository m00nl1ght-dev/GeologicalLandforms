using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Water Flow", 408)]
public class NodeOutputWaterFlow : NodeOutputBase
{
    public const string ID = "outputWaterFlow";
    public override string GetID => ID;

    public override string Title => "Water Flow";

    public override ValueConnectionKnob InputKnobRef => RiverTerrainKnob;

    [ValueConnectionKnob("River Terrain", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob RiverTerrainKnob;

    [ValueConnectionKnob("Flow Alpha", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob FlowAlphaKnob;

    [ValueConnectionKnob("Flow Beta", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob FlowBetaKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(RiverTerrainKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        RiverTerrainKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(FlowAlphaKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        FlowAlphaKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(FlowBetaKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        FlowBetaKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputWaterFlow;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputWaterFlow = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputWaterFlow == this) Landform.OutputWaterFlow = null;
    }

    public IGridFunction<TerrainData> GetRiverTerrain()
    {
        return RiverTerrainKnob.GetValue<ISupplier<IGridFunction<TerrainData>>>()?.ResetAndGet();
    }

    public IGridFunction<double> GetFlowAlpha()
    {
        return FlowAlphaKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }

    public IGridFunction<double> GetFlowBeta()
    {
        return FlowBetaKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }
}

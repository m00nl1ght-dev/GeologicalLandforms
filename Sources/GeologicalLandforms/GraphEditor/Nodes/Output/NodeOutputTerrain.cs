using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Terrain", 402)]
public class NodeOutputTerrain : NodeOutputBase
{
    public const string ID = "outputTerrain";
    public override string GetID => ID;

    public override string Title => "Terrain Output";

    public override ValueConnectionKnob InputKnobRef => BaseKnob;

    [ValueConnectionKnob("Base", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob BaseKnob;

    [ValueConnectionKnob("Stone", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob StoneKnob;

    [ValueConnectionKnob("Cave", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob CaveKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(BaseKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        BaseKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(StoneKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        StoneKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(CaveKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        CaveKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputTerrain;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputTerrain = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputTerrain == this) Landform.OutputTerrain = null;
    }

    public IGridFunction<TerrainDef> GetBase()
    {
        return BaseKnob.GetValue<ISupplier<IGridFunction<TerrainDef>>>()?.ResetAndGet();
    }

    public IGridFunction<TerrainDef> GetStone()
    {
        return StoneKnob.GetValue<ISupplier<IGridFunction<TerrainDef>>>()?.ResetAndGet();
    }

    public IGridFunction<TerrainDef> GetCave()
    {
        return CaveKnob.GetValue<ISupplier<IGridFunction<TerrainDef>>>()?.ResetAndGet();
    }
}

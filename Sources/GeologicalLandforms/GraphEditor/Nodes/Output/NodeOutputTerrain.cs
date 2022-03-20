using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

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

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputTerrain existing = Landform.OutputTerrain;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputTerrain = this;
    }

    public IGridFunction<TerrainData> GetBase()
    {
        IGridFunction<TerrainData> function = BaseKnob.GetValue<ISupplier<IGridFunction<TerrainData>>>()?.ResetAndGet();
        return function == null ? TerrainData.EmptyGrid : ScaleWithMap(function);
    }
    
    public IGridFunction<TerrainData> GetStone()
    {
        IGridFunction<TerrainData> function = StoneKnob.GetValue<ISupplier<IGridFunction<TerrainData>>>()?.ResetAndGet();
        return function == null ? TerrainData.EmptyGrid : ScaleWithMap(function);
    }
}
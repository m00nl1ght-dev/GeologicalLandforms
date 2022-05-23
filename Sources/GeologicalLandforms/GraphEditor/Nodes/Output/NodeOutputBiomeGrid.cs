using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Biome Grid", 403)]
public class NodeOutputBiomeGrid : NodeOutputBase
{
    public const string ID = "outputBiomeGrid";
    public override string GetID => ID;

    public override string Title => "Biome Output";

    public override ValueConnectionKnob InputKnobRef => BiomeGridKnob;

    [ValueConnectionKnob("Biome Grid", Direction.In, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob BiomeGridKnob;
    
    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(BiomeGridKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        BiomeGridKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeOutputBiomeGrid existing = Landform.OutputBiomeGrid;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputBiomeGrid = this;
    }
    
    protected override void OnDelete()
    {
        if (Landform.OutputBiomeGrid == this) Landform.OutputBiomeGrid = null;
    }

    public IGridFunction<BiomeData> GetBiomeGrid()
    {
        IGridFunction<BiomeData> function = BiomeGridKnob.GetValue<ISupplier<IGridFunction<BiomeData>>>()?.ResetAndGet();
        return function == null ? null : ScaleWithMap(function);
    }
}
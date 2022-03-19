using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Biome", 310)]
public class NodeTerrainFromBiome : NodeBase
{
    public const string ID = "terrainFromBiome";
    public override string GetID => ID;

    public override string Title => "Biome Terrain";
    
    [ValueConnectionKnob("Fertility", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob FertilityKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public double Fertility;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        KnobValueField(FertilityKnob, ref Fertility);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }
    
    public override void RefreshPreview()
    {
        RefreshIfConnected(FertilityKnob, ref Fertility);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<TerrainData>>(new Output(
            SupplierOrValueFixed(FertilityKnob, Fertility),
            Landform.GeneratingTile.Biome
        ));
        return true;
    }
    
    private class Output : ISupplier<TerrainData>
    {
        private readonly ISupplier<double> _fertility;
        private readonly BiomeDef _biome;

        public Output(ISupplier<double> fertility, BiomeDef biome)
        {
            _fertility = fertility;
            _biome = biome;
        }

        public TerrainData Get()
        {
            return new TerrainData(TerrainThreshold.TerrainAtValue(_biome.terrainsByFertility, (float) _fertility.Get()));
        }

        public void ResetState()
        {
            _fertility.ResetState();
        }
    }
}
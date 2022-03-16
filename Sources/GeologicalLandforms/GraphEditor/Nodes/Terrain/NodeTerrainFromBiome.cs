using System;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace TerrainGraph;

[Serializable]
[Node(false, "Terrain/Biome")]
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
        OutputKnob.SetValue<ISupplier<TerrainDef>>(new Output(
            SupplierOrValueFixed(FertilityKnob, Fertility),
            Landform.GeneratingBiome
        ));
        return true;
    }
    
    private class Output : ISupplier<TerrainDef>
    {
        private readonly ISupplier<double> _fertility;
        private readonly BiomeDef _biome;

        public Output(ISupplier<double> fertility, BiomeDef biome)
        {
            _fertility = fertility;
            _biome = biome;
        }

        public TerrainDef Get()
        {
            return TerrainThreshold.TerrainAtValue(_biome.terrainsByFertility, (float) _fertility.Get());
        }

        public void ResetState()
        {
            _fertility.ResetState();
        }
    }
}
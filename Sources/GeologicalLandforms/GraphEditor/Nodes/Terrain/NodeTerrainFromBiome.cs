using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Biome", 311)]
public class NodeTerrainFromBiome : NodeBase
{
    public const string ID = "terrainFromBiome";
    public override string GetID => ID;

    public override string Title => "Biome Terrain";

    [ValueConnectionKnob("Biome", Direction.In, BiomeFunctionConnection.Id)]
    public ValueConnectionKnob BiomeKnob;

    [ValueConnectionKnob("Fertility", Direction.In, ValueFunctionConnection.Id)]
    public ValueConnectionKnob FertilityKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string Biome;
    public double Fertility;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(BiomeKnob.name, BoxLayout);
        GUILayout.FlexibleSpace();
        BiomeData.BiomeSelector(this, Biome, !BiomeKnob.connected(), selected =>
        {
            Biome = BiomeData.ToString(selected);
            canvas.OnNodeChange(this);
        });
        GUILayout.EndHorizontal();
        BiomeKnob.SetPosition();

        KnobValueField(FertilityKnob, ref Fertility);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override void RefreshPreview()
    {
        ISupplier<BiomeData> biome = GetIfConnected<BiomeData>(BiomeKnob);
        ISupplier<double> fert = GetIfConnected<double>(FertilityKnob);
        if (biome != null) Biome = biome.ResetAndGet().ToString();
        if (fert != null) Fertility = fert.ResetAndGet();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<TerrainData>>(new Output(
            SupplierOrValueFixed(FertilityKnob, Fertility),
            SupplierOrFixed(BiomeKnob, BiomeData.FromString(Biome, Landform.GeneratingTile.Biome))
        ));
        return true;
    }

    private class Output : ISupplier<TerrainData>
    {
        private readonly ISupplier<double> _fertility;
        private readonly ISupplier<BiomeData> _biome;

        public Output(ISupplier<double> fertility, ISupplier<BiomeData> biome)
        {
            _fertility = fertility;
            _biome = biome;
        }

        public TerrainData Get()
        {
            var biomeData = _biome.Get();
            if (biomeData.IsEmpty) return TerrainData.Empty;
            return new TerrainData(TerrainThreshold.TerrainAtValue(biomeData.Biome.terrainsByFertility, (float) _fertility.Get()));
        }

        public void ResetState()
        {
            _biome.ResetState();
            _fertility.ResetState();
        }
    }
}

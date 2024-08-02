using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

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

        BiomeFunctionConnection.Instance.SelectorUI(this, Biome, !BiomeKnob.connected(), selected =>
        {
            Biome = BiomeFunctionConnection.Instance.ToString(selected);
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
        var biome = GetIfConnected<BiomeDef>(BiomeKnob);
        var fert = GetIfConnected<double>(FertilityKnob);

        biome?.ResetState();
        fert?.ResetState();

        if (biome != null) Biome = biome.Get().ToString();
        if (fert != null) Fertility = fert.Get();
    }

    public override void CleanUpGUI()
    {
        if (BiomeKnob.connected()) Biome = null;
        if (FertilityKnob.connected()) Fertility = 0;
    }

    public override bool Calculate()
    {
        var biome = BiomeFunctionConnection.Instance.FromString(Biome) ?? Landform.GeneratingTile.Biome;

        OutputKnob.SetValue<ISupplier<TerrainDef>>(new Output(
            SupplierOrFallback(FertilityKnob, Fertility),
            SupplierOrFallback(BiomeKnob, biome)
        ));

        return true;
    }

    private class Output : ISupplier<TerrainDef>
    {
        private readonly ISupplier<double> _fertility;
        private readonly ISupplier<BiomeDef> _biome;

        public Output(ISupplier<double> fertility, ISupplier<BiomeDef> biome)
        {
            _fertility = fertility;
            _biome = biome;
        }

        public TerrainDef Get()
        {
            var biomeDef = _biome.Get();
            if (biomeDef == null) return null;
            return TerrainThreshold.TerrainAtValue(biomeDef.terrainsByFertility, (float) _fertility.Get());
        }

        public void ResetState()
        {
            _biome.ResetState();
            _fertility.ResetState();
        }
    }
}

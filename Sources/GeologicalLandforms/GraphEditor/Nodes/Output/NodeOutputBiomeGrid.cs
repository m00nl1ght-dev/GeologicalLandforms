using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Biome Grid", 404)]
public class NodeOutputBiomeGrid : NodeOutputBase
{
    public const string ID = "outputBiomeGrid";
    public override string GetID => ID;

    public override string Title => "Biome Output";

    public override ValueConnectionKnob InputKnobRef => BiomeGridKnob;

    [ValueConnectionKnob("Biome Grid", Direction.In, BiomeGridFunctionConnection.Id)]
    public ValueConnectionKnob BiomeGridKnob;

    [ValueConnectionKnob("Transitions", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob BiomeTransitionKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(BiomeGridKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        BiomeGridKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(BiomeTransitionKnob.name, BoxLayout);
        GUILayout.EndHorizontal();
        BiomeTransitionKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputBiomeGrid;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputBiomeGrid = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputBiomeGrid == this) Landform.OutputBiomeGrid = null;
    }

    public IGridFunction<BiomeData> GetBiomeGrid()
    {
        return BiomeGridKnob.GetValue<ISupplier<IGridFunction<BiomeData>>>()?.ResetAndGet();
    }

    public IGridFunction<BiomeData> ApplyBiomeTransitions(IWorldTileInfo tile, IntVec2 mapSize, IGridFunction<BiomeData> landformBiomes)
    {
        var transition = BiomeTransitionKnob.GetValue<ISupplier<IGridFunction<double>>>();
        if (transition == null) return null;
        return new BiomeBorderFunc(landformBiomes, transition, tile, mapSize);
    }

    private class BiomeBorderFunc : IGridFunction<BiomeData>
    {
        private readonly IGridFunction<BiomeData> _preFunc;
        private readonly BiomeDef _primary;

        private readonly IGridFunction<double>[] _selFuncs;
        private readonly BiomeDef[] _biomes;

        public BiomeBorderFunc(
            IGridFunction<BiomeData> preFunc,
            ISupplier<IGridFunction<double>> selSupplier,
            IWorldTileInfo tile, IntVec2 mapSize)
        {
            _preFunc = preFunc;
            _primary = tile.Biome;
            _biomes = new BiomeDef[tile.BorderingBiomes.Count];
            _selFuncs = new IGridFunction<double>[_biomes.Length];

            selSupplier.ResetState();
            for (var i = 0; i < _biomes.Length; i++)
            {
                IWorldTileInfo.BorderingBiome borderingBiome = tile.BorderingBiomes[i];
                _biomes[i] = borderingBiome.Biome;

                var func = selSupplier.Get();
                func = new GridFunction.Rotate<double>(func, mapSize.x / 2f, mapSize.z / 2f, borderingBiome.Angle + 90f);
                _selFuncs[i] = func;
            }
        }

        public BiomeData ValueAt(double x, double z)
        {
            BiomeDef pre = _preFunc?.ValueAt(x, z).Biome;
            if (pre != null) return new BiomeData(pre);

            double v = 0;
            BiomeDef b = _primary;
            for (var i = 0; i < _biomes.Length; i++)
            {
                var sel = _selFuncs[i].ValueAt(x, z);
                if (sel > v)
                {
                    v = sel;
                    b = _biomes[i];
                }
            }

            return new BiomeData(b);
        }
    }
}

using System;
using LunarFramework.Utility;
using MapPreview;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;
using static TerrainGraph.GridFunction;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
public abstract class NodeInputBase : NodeBase
{
    public Landform Landform => (Landform) canvas;

    public virtual ValueConnectionKnob KnobRef => null;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(KnobRef.name, BoxLayout);
        GUILayout.EndHorizontal();
        KnobRef.SetPosition();

        GUILayout.EndVertical();
    }

    protected RandInstance TryGetVanillaGenStepRand(int seedPart, uint iteration = 0U)
    {
        if (Landform.GeneratingTile is WorldTileInfo tile)
        {
            return new RandInstance(Gen.HashCombineInt(SeedRerollData.GetMapSeed(tile.World, tile.TileId), seedPart), iteration);
        }

        return new RandInstance(Gen.HashCombineInt(CombinedSeed, seedPart), iteration);
    }

    /// <summary>
    /// Returns a vanilla elevation grid for the given tile in map space.
    /// </summary>
    public static IGridFunction<double> BuildVanillaElevationGrid(IWorldTileInfo tile, int seed)
    {
        IGridFunction<double> function = new NodeGridPerlin.NoiseFunction(0.021, 2, 0.5, 6, seed);
        function = new ScaleWithBias(function, 0.5, 0.5);
        function = new Multiply(function, Of(NodeValueWorldTile.GetHillinessFactor(tile.Hilliness)));
        if (tile.Elevation <= 0) function = new Min(function, Of<double>(0f));
        return function;
    }

    /// <summary>
    /// Returns a supplier that produces a vanilla elevation grid in node space.
    /// Uses the vanilla seed if an actual world tile is generating, otherwise the node seed.
    /// </summary>
    protected ISupplier<IGridFunction<double>> BuildVanillaElevationGridSupplier()
    {
        var seed = TryGetVanillaGenStepRand(826504671).Range(0, int.MaxValue);
        return new VanillaElevationGridSupplier(Landform.GeneratingTile, Landform.MapSpaceToNodeSpaceFactor, seed);
    }

    private class VanillaElevationGridSupplier : ISupplier<IGridFunction<double>>
    {
        private readonly IWorldTileInfo _tile;
        private readonly double _transformScale;
        private readonly int _seed;

        public VanillaElevationGridSupplier(IWorldTileInfo tile, double transformScale, int seed)
        {
            _tile = tile;
            _transformScale = transformScale;
            _seed = seed;
        }

        public IGridFunction<double> Get()
        {
            return new Transform<double>(BuildVanillaElevationGrid(_tile, _seed), _transformScale);
        }

        public void ResetState() { }
    }

    /// <summary>
    /// Returns a vanilla fertility grid for the given tile in map space.
    /// </summary>
    public static IGridFunction<double> BuildVanillaFertilityGrid(IWorldTileInfo tile, int seed)
    {
        IGridFunction<double> function = new NodeGridPerlin.NoiseFunction(0.021, 2, 0.5, 6, seed);
        function = new ScaleWithBias(function, 0.5, 0.5);
        return function;
    }

    /// <summary>
    /// Returns a supplier that produces a vanilla fertility grid in node space.
    /// Uses the vanilla seed if an actual world tile is generating, otherwise the node seed.
    /// </summary>
    protected ISupplier<IGridFunction<double>> BuildVanillaFertilityGridSupplier()
    {
        var seed = TryGetVanillaGenStepRand(826504671, 1).Range(0, int.MaxValue);
        return new VanillaFertilityGridSupplier(Landform.GeneratingTile, Landform.MapSpaceToNodeSpaceFactor, seed);
    }

    private class VanillaFertilityGridSupplier : ISupplier<IGridFunction<double>>
    {
        private readonly IWorldTileInfo _tile;
        private readonly double _transformScale;
        private readonly int _seed;

        public VanillaFertilityGridSupplier(IWorldTileInfo tile, double transformScale, int seed)
        {
            _tile = tile;
            _transformScale = transformScale;
            _seed = seed;
        }

        public IGridFunction<double> Get()
        {
            return new Transform<double>(BuildVanillaFertilityGrid(_tile, _seed), _transformScale);
        }

        public void ResetState() { }
    }

    /// <summary>
    /// Returns a supplier that produces a vanilla cave grid in node space.
    /// Elevation values are pulled from the output of the full landform stack.
    /// This can lead to infinite loops in the landform stack. Safeguards against this are implemented in the supplier.
    /// Uses the vanilla seed if an actual world tile is generating, otherwise the node seed.
    /// </summary>
    protected ISupplier<IGridFunction<double>> BuildVanillaCaveGridSupplier()
    {
        var caveGenRand = TryGetVanillaGenStepRand(647814558);
        var vanillaElevationSupplier = BuildVanillaElevationGridSupplier();
        return new VanillaCaveGridSupplier(vanillaElevationSupplier, Landform.MapSpaceToNodeSpaceFactor, Landform.GeneratingMapSize, caveGenRand);
    }

    private class VanillaCaveGridSupplier : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<IGridFunction<double>> _fallbackElevation;
        private readonly double _transformScale;
        private readonly IntVec2 _caveGridSize;
        private readonly RandInstance _rand;

        private bool _reentryFlag;

        public VanillaCaveGridSupplier(ISupplier<IGridFunction<double>> fallbackElevation, double transformScale, IntVec2 caveGridSize, RandInstance rand)
        {
            _fallbackElevation = fallbackElevation;
            _transformScale = transformScale;
            _caveGridSize = caveGridSize;
            _rand = rand;
        }

        public IGridFunction<double> Get()
        {
            IGridFunction<double> elevation = null;
            if (_reentryFlag)
            {
                GeologicalLandformsAPI.Logger.Error("Detected infinite loop in node graph! Using vanilla elevation as fallback.");
            }
            else
            {
                _reentryFlag = true;
                elevation = Landform.GetFeature(l => l.OutputElevation?.Get());
                _reentryFlag = false;
            }

            elevation ??= _fallbackElevation.Get();

            var generator = new TunnelGenerator();
            var elevationGrid = new Transform<double>(elevation, 1 / _transformScale);
            var cavesGrid = generator.Generate(_caveGridSize, _rand, c => elevationGrid.ValueAt(c.x, c.z) > 0.7).CavesGrid;
            return new Transform<double>(new Cache<double>(cavesGrid), _transformScale);
        }

        public void ResetState()
        {
            _fallbackElevation.ResetState();
        }
    }
}

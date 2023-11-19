using System;
using System.Collections.Generic;
using LunarFramework.Utility;
using NodeEditorFramework;
using RimWorld.Planet;
using TerrainGraph;
using TerrainGraph.Util;
using UnityEngine;
using Verse;
using static TerrainGraph.GridFunction;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Grid/Tunnels", 213)]
public class NodeGridTunnels : NodeBase
{
    public Landform Landform => (Landform) canvas;

    public const string ID = "gridTunnels";
    public override string GetID => ID;

    public override string Title => "Tunnels";

    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    [ValueConnectionKnob("Depths", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob DepthsKnob;

    [ValueConnectionKnob("Offsets", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OffsetsKnob;

    public double InputThreshold = 0.7;

    public double OpenTunnelsPer10K = 5.8;
    public double ClosedTunnelsPer10K = 2.5;

    public int MaxOpenTunnelsPerRockGroup = 3;
    public int MaxClosedTunnelsPerRockGroup = 1;

    public double TunnelWidthMultiplierMin = 0.8;
    public double TunnelWidthMultiplierMax = 1;

    public double WidthReductionPerCell = 0.034;

    public double BranchChance = 0.1;
    public int BranchMinDistanceFromStart = 15;
    public double BranchWidthOffsetMultiplier = 1;

    public double DirectionChangeSpeed = 8;

    public int MinEdgeCells;

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
        DepthsKnob.SetPosition(FirstKnobPosition + 28);
        OffsetsKnob.SetPosition(FirstKnobPosition + 56);

        GUILayout.BeginVertical(BoxStyle);

        ValueField("Input threshold", ref InputThreshold);

        ValueField("Open per 10K", ref OpenTunnelsPer10K);
        ValueField("Closed per 10K", ref ClosedTunnelsPer10K);

        IntField("Max open", ref MaxOpenTunnelsPerRockGroup);
        IntField("Max closed", ref MaxClosedTunnelsPerRockGroup);

        ValueField("Width min", ref TunnelWidthMultiplierMin);
        ValueField("Width max", ref TunnelWidthMultiplierMax);
        ValueField("Loss per cell", ref WidthReductionPerCell);

        ValueField("Branch chance", ref BranchChance);
        IntField("Branch min dist", ref BranchMinDistanceFromStart);
        ValueField("Branch width", ref BranchWidthOffsetMultiplier);

        ValueField("Angle change", ref DirectionChangeSpeed);

        IntField("Min edge cells", ref MinEdgeCells);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        var output = new Output(
            SupplierOrGridFixed(InputKnob, Zero), InputThreshold,
            new TunnelGenerator
            {
                OpenTunnelsPer10K = (float) OpenTunnelsPer10K,
                ClosedTunnelsPer10K = (float) ClosedTunnelsPer10K,
                MaxOpenTunnelsPerRockGroup = MaxOpenTunnelsPerRockGroup,
                MaxClosedTunnelsPerRockGroup = MaxClosedTunnelsPerRockGroup,
                TunnelWidthMultiplierMin = (float) TunnelWidthMultiplierMin,
                TunnelWidthMultiplierMax = (float) TunnelWidthMultiplierMax,
                WidthReductionPerCell = (float) WidthReductionPerCell,
                BranchChance = (float) BranchChance,
                BranchMinDistanceFromStart = BranchMinDistanceFromStart,
                BranchWidthOffsetMultiplier = (float) BranchWidthOffsetMultiplier,
                DirectionChangeSpeed = (float) DirectionChangeSpeed
            },
            Landform.MapSpaceToNodeSpaceFactor, Landform.GeneratingMapSize,
            CombinedSeed, TerrainCanvas.CreateRandomInstance(),
            DepthsKnob.connected(), OffsetsKnob.connected(),
            MinEdgeCells
        );

        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(Supplier.From(output.GetCaves, output.ResetState));
        DepthsKnob.SetValue<ISupplier<IGridFunction<double>>>(Supplier.From(output.GetDepths, output.ResetState));
        OffsetsKnob.SetValue<ISupplier<IGridFunction<double>>>(Supplier.From(output.GetOffsets, output.ResetState));
        return true;
    }

    private class Output
    {
        private readonly ISupplier<IGridFunction<double>> _input;
        private readonly double _inputThreshold;
        private readonly TunnelGenerator _generator;
        private readonly double _transformScale;
        private readonly IntVec2 _targetGridSize;
        private readonly int _seed;
        private readonly IRandom _random;
        private readonly bool _doDepths;
        private readonly bool _doOffsets;
        private readonly int _minEdgeCells;

        private TunnelGenerator.Result _result;

        public Output(
            ISupplier<IGridFunction<double>> input, double inputThreshold, TunnelGenerator generator,
            double transformScale, IntVec2 targetGridSize, int seed, IRandom random, bool doDepths, bool doOffsets, int minEdgeCells)
        {
            _input = input;
            _inputThreshold = inputThreshold;
            _generator = generator;
            _transformScale = transformScale;
            _targetGridSize = targetGridSize;
            _seed = seed;
            _random = random;
            _doDepths = doDepths;
            _doOffsets = doOffsets;
            _minEdgeCells = minEdgeCells;
            _random.Reinitialise(_seed);
        }

        private void GenerateIfNeeded()
        {
            const int maxTries = 10;

            if (_result.CavesGrid != null) return;

            var maxAccepted = 0;

            for (int i = 0; i < maxTries; i++)
            {
                var rand = new RandInstance(_random.Next());
                var input = new Transform<double>(_input.Get(), 1 / _transformScale);
                _result = _generator.Generate(_targetGridSize, rand, c => input.ValueAt(c.x, c.z) > _inputThreshold, _doDepths, _doOffsets);

                if (_minEdgeCells <= 0) return;

                var walkable = new StructRot4<int>();
                var function = new Cache<double>(_result.CavesGrid);

                walkable[Rot4.West] = NodeGridValidate.ValidateCol(function, 0, _targetGridSize.z, 1, 9999);
                walkable[Rot4.South] = NodeGridValidate.ValidateRow(function, 0, _targetGridSize.x, 1, 9999);
                walkable[Rot4.East] = NodeGridValidate.ValidateCol(function, _targetGridSize.x - 1, _targetGridSize.z, 1, 9999);
                walkable[Rot4.North] = NodeGridValidate.ValidateRow(function, _targetGridSize.z - 1, _targetGridSize.x, 1, 9999);

                var total = walkable[Rot4.West] + walkable[Rot4.South] + walkable[Rot4.East] + walkable[Rot4.North];
                if (total > maxAccepted) maxAccepted = total;

                if (total >= _minEdgeCells)
                {
                    if (Landform.GeneratingTile is WorldTileInfo tileInfo && !CheckMapSides(tileInfo, walkable)) continue;

                    GeologicalLandformsAPI.Logger.Debug($"Cave grid passed validation after {i + 1} tries, with {total} accepted cells.");
                    return;
                }
            }

            GeologicalLandformsAPI.Logger.Debug($"Cave grid failed validation after {maxTries} tries, with {maxAccepted} accepted cells.");
        }

        [ThreadStatic]
        private static List<int> _tscNeighbors;

        private bool CheckMapSides(WorldTileInfo tileInfo, StructRot4<int> walkable)
        {
            tileInfo.World.grid.GetTileNeighbors(tileInfo.TileId, _tscNeighbors ??= new());
            _tscNeighbors.RemoveAll(nb => tileInfo.World.Impassable(nb));

            foreach (var nb in _tscNeighbors)
            {
                CaravanExitMapUtility.GetExitMapEdges(tileInfo.World.grid, tileInfo.TileId, nb, out var primary, out var secondary);
                if ((primary.IsValid && walkable[primary] >= 3) || (secondary.IsValid && walkable[secondary] >= 3)) return true;
            }

            return _tscNeighbors.Count == 0;
        }

        public IGridFunction<double> GetCaves()
        {
            GenerateIfNeeded();
            if (_result.CavesGrid == null) return Zero;
            return new Transform<double>(new Cache<double>(_result.CavesGrid), _transformScale);
        }

        public IGridFunction<double> GetDepths()
        {
            GenerateIfNeeded();
            if (_result.DepthGrid == null) return Zero;
            return new Transform<double>(new Cache<double>(_result.DepthGrid), _transformScale);
        }

        public IGridFunction<double> GetOffsets()
        {
            GenerateIfNeeded();
            if (_result.OffsetGrid == null) return Zero;
            return new Transform<double>(new Cache<double>(_result.OffsetGrid), _transformScale);
        }

        public void ResetState() // TODO investigate: this might cause it to be generated multiple times if all grids are used by output nodes
        {
            _result = new TunnelGenerator.Result();
            _random.Reinitialise(_seed);
            _input.ResetState();
        }
    }
}

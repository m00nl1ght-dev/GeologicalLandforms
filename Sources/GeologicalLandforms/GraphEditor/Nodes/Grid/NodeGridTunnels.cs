using System;
using NodeEditorFramework;
using TerrainGraph;
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
    
    public double InputThreshold = 0.7;
    
    public double OpenTunnelsPer10K = 5.8;
    public double ClosedTunnelsPer10K = 2.5;
    
    public int MaxOpenTunnelsPerRockGroup = 3;
    public int MaxClosedTunnelsPerRockGroup = 1;
    
    public double TunnelWidthMultiplier = 1;
    public double TunnelWidthMin = 1.4;
    public double WidthReductionPerCell = 0.034;
    
    public double BranchChance = 0.1;
    public int BranchMinDistanceFromStart = 15;
    public double BranchWidthOffsetMultiplier = 1;
    
    public double DirectionChangeSpeed = 8;
    
    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        ValueField("Input threshold", ref InputThreshold);

        ValueField("Open per 10K", ref OpenTunnelsPer10K);
        ValueField("Closed per 10K", ref ClosedTunnelsPer10K);
        
        IntField("Max open", ref MaxOpenTunnelsPerRockGroup);
        IntField("Max closed", ref MaxClosedTunnelsPerRockGroup);
        
        ValueField("Base width", ref TunnelWidthMultiplier);
        ValueField("Base width min", ref TunnelWidthMin);
        ValueField("Loss per cell", ref WidthReductionPerCell);
        
        ValueField("Branch chance", ref BranchChance);
        IntField("Branch min dist", ref BranchMinDistanceFromStart);
        ValueField("Branch width", ref BranchWidthOffsetMultiplier);
        
        ValueField("Angle change", ref DirectionChangeSpeed);

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrGridFixed(InputKnob, Zero), InputThreshold,
            new TunnelGenerator
            {
                OpenTunnelsPer10K = (float) OpenTunnelsPer10K,
                ClosedTunnelsPer10K = (float) ClosedTunnelsPer10K,
                MaxOpenTunnelsPerRockGroup = MaxOpenTunnelsPerRockGroup,
                MaxClosedTunnelsPerRockGroup = MaxClosedTunnelsPerRockGroup,
                TunnelWidthMultiplier = (float) TunnelWidthMultiplier,
                TunnelWidthMin = (float) TunnelWidthMin,
                WidthReductionPerCell = (float) WidthReductionPerCell,
                BranchChance = (float) BranchChance,
                BranchMinDistanceFromStart = BranchMinDistanceFromStart,
                BranchWidthOffsetMultiplier = (float) BranchWidthOffsetMultiplier,
                DirectionChangeSpeed = (float) DirectionChangeSpeed
            },
            Landform.MapSpaceToNodeSpaceFactor, 
            Landform.GeneratingMapSize, 
            CombinedSeed
        ));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<IGridFunction<double>> _input;
        private readonly double _inputThreshold;
        private readonly TunnelGenerator _generator;
        private readonly double _transformScale;
        private readonly IntVec2 _targetGridSize;
        private readonly int _seed;

        public Output(ISupplier<IGridFunction<double>> input, double inputThreshold, 
            TunnelGenerator generator, double transformScale, IntVec2 targetGridSize, int seed)
        {
            _input = input;
            _inputThreshold = inputThreshold;
            _generator = generator;
            _transformScale = transformScale;
            _targetGridSize = targetGridSize;
            _seed = seed;
        }

        public IGridFunction<double> Get()
        {
            if (Landform.GeneratingTile is WorldTileInfo || Input.GetKey(KeyCode.LeftShift))
            {
                var input = new Transform<double>(_input.Get(), 1 / _transformScale);
                var grid = _generator.Generate(_targetGridSize, _seed, c => input.ValueAt(c.x, c.z) > _inputThreshold);
                return new Transform<double>(new Cache<double>(grid), _transformScale);
            }

            return One; // TODO allow in editor once async previews are implemented
        }

        public void ResetState()
        {
            _input.ResetState();
        }
    }
}
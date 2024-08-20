using System;
using LunarFramework.Utility;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Natural Priority", 312)]
public class NodeTerrainNaturalPriority : NodeBase
{
    public const string ID = "terrainNaturalPriority";
    public override string GetID => ID;

    public override string Title => "Natural Priority";

    [ValueConnectionKnob("Input A", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob InputAKnob;

    [ValueConnectionKnob("Input B", Direction.In, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob InputBKnob;

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Input A", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InputAKnob.SetPosition();

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Input B", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        InputBKnob.SetPosition();

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainDef>>>(new Output(
            SupplierOrFallback(InputBKnob, GridFunction.Of<TerrainDef>(null)),
            SupplierOrFallback(InputAKnob, GridFunction.Of<TerrainDef>(null))
        ));

        return true;
    }

    private class Output : ISupplier<IGridFunction<TerrainDef>>
    {
        private readonly ISupplier<IGridFunction<TerrainDef>> _inputA;
        private readonly ISupplier<IGridFunction<TerrainDef>> _inputB;

        public Output(ISupplier<IGridFunction<TerrainDef>> inputA, ISupplier<IGridFunction<TerrainDef>> inputB)
        {
            _inputA = inputA;
            _inputB = inputB;
        }

        public IGridFunction<TerrainDef> Get()
        {
            return new Function(_inputA.Get(), _inputB.Get());
        }

        public void ResetState()
        {
            _inputB.ResetState();
            _inputA.ResetState();
        }
    }

    private class Function : IGridFunction<TerrainDef>
    {
        private readonly IGridFunction<TerrainDef> _inputA;
        private readonly IGridFunction<TerrainDef> _inputB;

        public Function(IGridFunction<TerrainDef> inputA, IGridFunction<TerrainDef> inputB)
        {
            _inputA = inputA;
            _inputB = inputB;
        }

        public TerrainDef ValueAt(double x, double z)
        {
            var a = _inputA.ValueAt(x, z);
            var b = _inputB.ValueAt(x, z);

            if (a == null) return b;
            if (b == null) return a;

            if (a.IsDeepWater()) return a;
            if (b.IsDeepWater()) return b;

            if (a == TerrainDefOf.WaterMovingChestDeep) return a;
            if (b == TerrainDefOf.WaterMovingChestDeep) return b;

            if (a == TerrainDefOf.WaterShallow || a == TerrainDefOf.WaterOceanShallow) return a;
            if (b == TerrainDefOf.WaterShallow || b == TerrainDefOf.WaterOceanShallow) return b;

            if (a.IsRiver) return a;
            if (b.IsRiver) return b;

            return a.fertility >= b.fertility ? a : b;
        }
    }
}

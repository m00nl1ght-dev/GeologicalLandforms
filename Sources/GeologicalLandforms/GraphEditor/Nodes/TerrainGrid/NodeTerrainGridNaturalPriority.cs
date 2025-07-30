using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

#if RW_1_6_OR_GREATER
using static GeologicalLandforms.TerrainPriority;
#else
using LunarFramework.Utility;
using RimWorld;
#endif

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Grid/Natural Priority", 312)]
public class NodeTerrainGridNaturalPriority : NodeBase
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

    public PriorityOptions Options = new();

    #if !RW_1_6_OR_GREATER

    public struct PriorityOptions
    {
        public bool PrioritizeWater = true;
        public bool PrioritizeIce = false;
        public bool InvertFertility = false;

        public PriorityOptions() {}
    }

    #endif

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);

        GUILayout.BeginVertical(BoxStyle);

        KnobLabelDouble(InputAKnob, "Input A");
        KnobLabelDouble(InputBKnob, "Input B");

        GUILayout.BeginVertical(GUI.skin.box);

        Options.PrioritizeWater = GUILayout.Toggle(Options.PrioritizeWater, "  Prioritize water");

        #if RW_1_6_OR_GREATER
        GUI.enabled = Options.PrioritizeWater;
        Options.PrioritizeMovingWater = GUILayout.Toggle(Options.PrioritizeMovingWater, "  Prioritize moving water");
        GUI.enabled = true;
        #endif

        Options.PrioritizeIce = GUILayout.Toggle(Options.PrioritizeIce, "  Prioritize ice");

        #if RW_1_6_OR_GREATER
        Options.PrioritizeFertility = GUILayout.Toggle(Options.PrioritizeFertility, "  Prioritize fertility");
        GUI.enabled = Options.PrioritizeFertility;
        Options.InvertFertility = GUILayout.Toggle(Options.InvertFertility, "  Invert fertility");
        GUI.enabled = true;
        #else
        Options.InvertFertility = GUILayout.Toggle(Options.InvertFertility, "  Invert fertility");
        #endif

        GUILayout.Space(3f);
        GUILayout.EndVertical();

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        SetOutput(OutputKnob, new Output(
            SupplierOrFallback(InputAKnob, GridFunction.Of<TerrainDef>(null)),
            SupplierOrFallback(InputBKnob, GridFunction.Of<TerrainDef>(null)),
            Options
        ));

        return true;
    }

    private class Output : ISupplier<IGridFunction<TerrainDef>>
    {
        private readonly ISupplier<IGridFunction<TerrainDef>> _inputA;
        private readonly ISupplier<IGridFunction<TerrainDef>> _inputB;
        private readonly PriorityOptions _options;

        public Output(
            ISupplier<IGridFunction<TerrainDef>> inputA,
            ISupplier<IGridFunction<TerrainDef>> inputB,
            PriorityOptions options)
        {
            _inputA = inputA;
            _inputB = inputB;
            _options = options;
        }

        public IGridFunction<TerrainDef> Get()
        {
            return new Function(_inputA.Get(), _inputB.Get(), _options);
        }

        public void ResetState()
        {
            _inputA.ResetState();
            _inputB.ResetState();
        }
    }

    private class Function : IGridFunction<TerrainDef>
    {
        private readonly IGridFunction<TerrainDef> _inputA;
        private readonly IGridFunction<TerrainDef> _inputB;
        private readonly PriorityOptions _options;

        public Function(
            IGridFunction<TerrainDef> inputA,
            IGridFunction<TerrainDef> inputB,
            PriorityOptions options)
        {
            _inputA = inputA;
            _inputB = inputB;
            _options = options;
        }

        public TerrainDef ValueAt(double x, double z)
        {
            var a = _inputA.ValueAt(x, z);
            var b = _inputB.ValueAt(x, z);

            #if RW_1_6_OR_GREATER

            return Apply(a, b, _options);

            #else

            if (b == null) return a;
            if (a == null) return b;

            if (_options.PrioritizeWater)
            {
                if (b.IsDeepWater()) return b;
                if (a.IsDeepWater()) return a;

                if (b == TerrainDefOf.WaterMovingChestDeep) return b;
                if (a == TerrainDefOf.WaterMovingChestDeep) return a;

                if (b == TerrainDefOf.WaterShallow || b == TerrainDefOf.WaterOceanShallow) return b;
                if (a == TerrainDefOf.WaterShallow || a == TerrainDefOf.WaterOceanShallow) return a;

                if (b.IsRiver) return b;
                if (a.IsRiver) return a;
            }

            if (_options.PrioritizeIce)
            {
                if (b == TerrainDefOf.Ice || a == TerrainDefOf.Ice) return TerrainDefOf.Ice;
            }

            return (b.fertility >= a.fertility) ^ _options.InvertFertility ? b : a;

            #endif
        }
    }
}

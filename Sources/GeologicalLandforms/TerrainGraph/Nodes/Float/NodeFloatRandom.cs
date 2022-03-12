using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Value/Random")]
public class NodeFloatRandom : NodeBase
{
    public const string ID = "floatRandom";
    public override string GetID => ID;

    public override string Title => "Random Value";
    public override Vector2 DefaultSize => new(200, 85);
    
    [ValueConnectionKnob("Min", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob MinKnob;
    
    [ValueConnectionKnob("Max", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob MaxKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ConnectionFloat.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public float MinValue;
    public float MaxValue = 1f;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        KnobFloatField(MinKnob, ref MinValue);
        OutputKnob.SetPosition();
        KnobFloatField(MaxKnob, ref MaxValue);

        GUILayout.EndVertical();

        if (GUI.changed)
            NodeEditor.curNodeCanvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<Func<float>>(GetValue);
        return true;
    }

    private float GetValue()
    {
        float min = MinKnob.connected() ? MinValue = MinKnob.GetValue<Func<float>>().Invoke() : MinValue;
        float max = MaxKnob.connected() ? MaxValue = MaxKnob.GetValue<Func<float>>().Invoke() : MaxValue;

        return Rand.Range(min, max);
    }
}
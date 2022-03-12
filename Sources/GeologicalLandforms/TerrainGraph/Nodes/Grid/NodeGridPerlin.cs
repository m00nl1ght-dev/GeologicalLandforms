using System;
using NodeEditorFramework;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Grid/Perlin Noise")]
public class NodeGridPerlin : NodeBase
{
    public const string ID = "gridPerlin";
    public override string GetID => ID;

    public override string Title => "Perlin Noise";
    public override Vector2 DefaultSize => new(200, 195);
    
    [ValueConnectionKnob("Frequency", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob FrequencyKnob;
    
    [ValueConnectionKnob("Lacunarity", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob LacunarityKnob;
    
    [ValueConnectionKnob("Persistence", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob PersistenceKnob;
    
    [ValueConnectionKnob("Scale", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob ScaleKnob;
    
    [ValueConnectionKnob("Bias", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob BiasKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ConnectionGridModule.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public float Frequency = 0.021f;
    public float Lacunarity = 2f;
    public float Persistence = 0.5f;
    public float Scale = 0.5f;
    public float Bias = 0.5f;
    
    public int Octaves = 6;
    
    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        KnobFloatField(FrequencyKnob, ref Frequency);
        OutputKnob.SetPosition();
        KnobFloatField(LacunarityKnob, ref Lacunarity);
        KnobFloatField(PersistenceKnob, ref Persistence);
        KnobFloatField(ScaleKnob, ref Scale);
        KnobFloatField(BiasKnob, ref Bias);
        IntField("Octaves", ref Octaves);
        
        GUILayout.EndVertical();

        if (GUI.changed)
            NodeEditor.curNodeCanvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<Func<ModuleBase>>(BuildModule);
        return true;
    }

    private ModuleBase BuildModule()
    {
        return new ScaleBias(
            ScaleKnob.connected() ? Scale = ScaleKnob.GetValue<Func<float>>().Invoke() : Scale,
            BiasKnob.connected() ? Bias = BiasKnob.GetValue<Func<float>>().Invoke() : Bias,
            new Perlin(
                FrequencyKnob.connected() ? Frequency = FrequencyKnob.GetValue<Func<float>>().Invoke() : Frequency,
                LacunarityKnob.connected() ? Lacunarity = LacunarityKnob.GetValue<Func<float>>().Invoke() : Lacunarity,
                PersistenceKnob.connected() ? Persistence = PersistenceKnob.GetValue<Func<float>>().Invoke() : Persistence,
                Octaves, Rand.Range(0, int.MaxValue), QualityMode.High
            ));
    }
}
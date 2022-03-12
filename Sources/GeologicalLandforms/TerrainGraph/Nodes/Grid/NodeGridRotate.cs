using System;
using NodeEditorFramework;
using UnityEngine;
using Verse.Noise;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Grid/Rotate")]
public class NodeGridRotate : NodeBase
{
    public const string ID = "gridRotate";
    public override string GetID => ID;

    public override string Title => "Rotate";
    public override Vector2 DefaultSize => new(200, 85);
    
    [ValueConnectionKnob("Input", Direction.In, ConnectionGridModule.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("Angle", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob AngleKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ConnectionGridModule.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public float Angle;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Input");
        InputKnob.SetPosition();
        OutputKnob.SetPosition();
        GUILayout.EndHorizontal();
        
        KnobFloatField(AngleKnob, ref Angle);

        GUILayout.EndVertical();

        if (GUI.changed)
            NodeEditor.curNodeCanvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<Func<ModuleBase>>(GetValue);
        return true;
    }

    private ModuleBase GetValue()
    {
        return new Rotate(
            0f, AngleKnob.connected() ? Angle = AngleKnob.GetValue<Func<float>>().Invoke() : Angle, 0f,
            InputKnob.GetValue<Func<ModuleBase>>()?.Invoke() ?? new Const(0f)
        );
    }
}
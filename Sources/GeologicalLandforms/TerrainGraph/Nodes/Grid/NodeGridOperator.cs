using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Grid/Operator")]
public class NodeGridOperator : NodeBase
{
    public const string ID = "gridOperator";
    public override string GetID => ID;

    public override string Title => OperationType.ToString();
    public override Vector2 MinSize => new(200, 10);
    public override bool AutoLayout => true;
    
    [ValueConnectionKnob("Apply chance", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob ApplyChanceKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ConnectionGridModule.Id)]
    public ValueConnectionKnob OutputKnob;

    public Operation OperationType = Operation.Add;
    public float Smoothness;
    public float ApplyChance = 1f;

    public override void NodeGUI()
    {
        void CreateNewKnob() => CreateValueConnectionKnob(new("Input " + dynamicConnectionPorts.Count, Direction.In, ConnectionGridModule.Id));

        while (dynamicConnectionPorts.Count < 2)
            CreateNewKnob();
        
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        
        if (GUILayout.Button("Operation", GUILayout.ExpandWidth(true)))
        {
            Dropdown<Operation>(o => OperationType = o);
        }

        GUI.enabled = dynamicConnectionPorts.Count < 20;
        if (GUILayout.Button("+", GUILayout.Width(20f)))
        {
            CreateNewKnob();
        }
        
        GUI.enabled = dynamicConnectionPorts.Count > 2;
        if (GUILayout.Button("-", GUILayout.Width(20f)))
        {
            DeleteConnectionPort(dynamicConnectionPorts.Count - 1);
        }
        
        GUI.enabled = true;
        OutputKnob.SetPosition();
        
        GUILayout.EndHorizontal();

        if (OperationType is Operation.Min or Operation.Max)
        {
            FloatField("Smoothness", ref Smoothness);
        }
        
        KnobFloatField(ApplyChanceKnob, ref ApplyChance);

        for (int i = 0; i < dynamicConnectionPorts.Count; i++)
        {
            GUILayout.BeginHorizontal(BoxStyle);
            GUILayout.Label(i == 0 ? "Base" : ("Input " + i));
            ((ValueConnectionKnob)dynamicConnectionPorts[i]).SetPosition();
            GUILayout.EndHorizontal();
        }

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
        float applyChance = ApplyChanceKnob.connected() ? ApplyChance = ApplyChanceKnob.GetValue<Func<float>>().Invoke() : ApplyChance;
        
        List<ModuleBase> inputs = new();
        for (int i = 0; i < dynamicConnectionPorts.Count; i++)
        {
            if (i > 0 && applyChance < 1f && !Rand.Chance(applyChance)) continue;
            
            ValueConnectionKnob port = (ValueConnectionKnob) dynamicConnectionPorts[i];
            if (port.connected())
            {
                inputs.Add(port.GetValue<Func<ModuleBase>>().Invoke());
            }
        }

        return inputs.Count switch
        {
            0 => new Const(0f),
            1 => inputs[0],
            _ => OperationType switch
            {
                Operation.Add => inputs.Aggregate((a, b) => new Add(a, b)),
                Operation.Multiply => inputs.Aggregate((a, b) => new Multiply(a, b)),
                Operation.Min => inputs.Aggregate((a, b) => new SmoothMin(a, b, Smoothness)),
                Operation.Max => inputs.Aggregate((a, b) => new SmoothMax(a, b, Smoothness)),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }

    public enum Operation
    {
        Add, Multiply, Min, Max
    }
}
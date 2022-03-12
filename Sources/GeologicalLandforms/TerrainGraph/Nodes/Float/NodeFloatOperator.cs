using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Value/Operator")]
public class NodeFloatOperator : NodeBase
{
    public const string ID = "floatOperator";
    public override string GetID => ID;

    public override string Title => OperationType.ToString();
    public override Vector2 MinSize => new(200, 10);
    public override bool AutoLayout => true;

    [ValueConnectionKnob("Apply chance", Direction.In, ConnectionFloat.Id)]
    public ValueConnectionKnob ApplyChanceKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, ConnectionFloat.Id)]
    public ValueConnectionKnob OutputKnob;

    public Operation OperationType = Operation.Add;
    public float Smoothness;
    public float ApplyChance = 1f;
    
    public List<float> Values = new();

    public override void NodeGUI()
    {
        void CreateNewKnob() => CreateValueConnectionKnob(new("Input " + dynamicConnectionPorts.Count, Direction.In, ConnectionFloat.Id));

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

        while (Values.Count < dynamicConnectionPorts.Count) Values.Add(0f);
        while (Values.Count > dynamicConnectionPorts.Count) Values.RemoveAt(Values.Count - 1);
        
        for (int i = 0; i < dynamicConnectionPorts.Count; i++)
        {
            ValueConnectionKnob knob = (ValueConnectionKnob)dynamicConnectionPorts[i];
            var value = Values[i];
            KnobFloatField(knob, ref value, i == 0 ? "Base" : ("Input " + i));
            Values[i] = value;
        }

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
        float applyChance = ApplyChanceKnob.connected() ? ApplyChance = ApplyChanceKnob.GetValue<Func<float>>().Invoke() : ApplyChance;
        
        List<float> inputs = new();
        for (int i = 0; i < Math.Min(Values.Count, dynamicConnectionPorts.Count); i++)
        {
            if (i > 0 && applyChance < 1f && !Rand.Chance(applyChance)) continue;
            
            ValueConnectionKnob port = (ValueConnectionKnob) dynamicConnectionPorts[i];
            if (port.connected())
            {
                inputs.Add(Values[i] = port.GetValue<Func<float>>().Invoke());
            }
            else
            {
                inputs.Add(Values[i]);
            }
        }

        return inputs.Count switch
        {
            0 => 0f,
            1 => inputs[0],
            _ => OperationType switch
            {
                Operation.Add => inputs.Aggregate((a, b) => a + b),
                Operation.Multiply => inputs.Aggregate((a, b) => a * b),
                Operation.Min => inputs.Aggregate((a, b) => (float) SmoothMin.Of(a, b, Smoothness)),
                Operation.Max => inputs.Aggregate((a, b) => (float) SmoothMax.Of(a, b, Smoothness)),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }

    public enum Operation
    {
        Add, Multiply, Min, Max
    }
}
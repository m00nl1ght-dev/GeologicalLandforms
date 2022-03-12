using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

public abstract class NodeBase : Node
{
    public Landform Landform => (Landform) canvas;

    private static GUIStyle _boxStyle;
    public static GUIStyle BoxStyle
    {
        get { 
            _boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(0, 0, 0, 0),
                normal = new GUIStyleState()
            };  
            return _boxStyle; 
        }
    }

    protected void KnobFloatField(ValueConnectionKnob knob, ref float value, string label = null)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label ?? knob.name, GUILayout.Width(DefaultSize.x / 2f));

        if (knob.connected())
        {
            GUILayout.Label(Math.Round(value, 2).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            value = RTEditorGUI.FloatField(GUIContent.none, value);
        }

        GUILayout.EndHorizontal();
        
        knob.SetPosition();
    }
    
    protected void FloatField(string label, ref float value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, GUILayout.Width(DefaultSize.x / 2f));
        
        value = RTEditorGUI.FloatField(GUIContent.none, value);
        
        GUILayout.EndHorizontal();
    }
    
    protected void IntField(string label, ref int value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, GUILayout.Width(DefaultSize.x / 2f));
        
        value = RTEditorGUI.IntField(GUIContent.none, value);
        
        GUILayout.EndHorizontal();
    }
    
    public static void Dropdown<T>(List<T> potentialValues, Action<T> action, Func<T, string> textFunc = null)
    {
        List<FloatMenuOption> options = potentialValues.Select(e => 
            new FloatMenuOption(textFunc != null ? textFunc.Invoke(e) : e.ToString(), () => action(e))).ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }

    public static void Dropdown<T>(Action<T> action, string translationKeyPrefix = null) where T : Enum
    {
        Dropdown(typeof(T).GetEnumValues().Cast<T>().ToList(), action, 
            translationKeyPrefix == null ? null : e => (translationKeyPrefix + "." + e).Translate());
    }
    
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;
using Random = System.Random;

namespace TerrainGraph;

[Serializable]
public abstract class NodeBase : Node
{
    public static readonly Random SeedSource = new();
    
    public TerrainCanvas TerrainCanvas => (TerrainCanvas) canvas;
    public double GridSize => TerrainCanvas.GridFullSize;
    
    public int RandSeed = SeedSource.Next();
    public int CombinedSeed => RandSeed ^ TerrainCanvas.RandSeed;

    public const int FirstKnobPosition = 37;

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
    
    public override void OnCreate(bool initGUI)
    {
        RefreshDynamicKnobs();
        if (initGUI) PrepareGUI();
    }

    public virtual void PrepareGUI() {}
    
    public virtual void CleanUpGUI() {}

    public virtual void RefreshPreview() {}
    
    public virtual void RefreshDynamicKnobs() {}

    protected void KnobValueField(ValueConnectionKnob knob, ref double value, string label = null)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label ?? knob.name, GUILayout.Width(DefaultSize.x / 2f));

        if (knob.connected())
        {
            GUILayout.Label(Math.Round(value, 2).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            value = RTEditorGUI.FloatField(GUIContent.none, (float) value);
        }

        GUILayout.EndHorizontal();
        
        knob.SetPosition();
    }
    
    protected void ValueField(string label, ref double value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, GUILayout.Width(DefaultSize.x / 2f));
        
        value = RTEditorGUI.FloatField(GUIContent.none, (float) value);
        
        GUILayout.EndHorizontal();
    }
    
    protected void IntField(string label, ref int value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, GUILayout.Width(DefaultSize.x / 2f));
        
        value = RTEditorGUI.IntField(GUIContent.none, value);
        
        GUILayout.EndHorizontal();
    }
    
    public delegate void DropdownHandler(List<string> potentialValues, Action<int> action);
    public static DropdownHandler ActiveDropdownHandler { get; set; }

    public void Dropdown<T>(List<T> potentialValues, Action<T> action, Func<T, string> textFunc = null)
    {
        ActiveDropdownHandler?.Invoke(potentialValues.Select(e => textFunc != null ? textFunc.Invoke(e) : e.ToString()).ToList(), i =>
        {
            action(potentialValues[i]);
            NodeEditor.curNodeCanvas.OnNodeChange(this);
        });
    }

    public void Dropdown<T>(Action<T> action) where T : Enum
    {
        Dropdown(typeof(T).GetEnumValues().Cast<T>().ToList(), action, e => e.ToString().Replace('_', ' '));
    }
    
    public void SelectionMenu<T>(GenericMenu menu, List<T> potentialValues, Action<T> action, Func<T, string> textFunc = null)
    {
        foreach (T value in potentialValues)
        {
            string text = textFunc != null ? textFunc.Invoke(value) : value.ToString();
            menu.AddItem(new GUIContent(text), false, () => action(value));
        }
    }

    public void SelectionMenu<T>(GenericMenu menu, Action<T> action, string prefix = "") where T : Enum
    {
        SelectionMenu(menu, typeof(T).GetEnumValues().Cast<T>().ToList(), action, e => prefix + e.ToString().Replace('_', ' '));
    }
    
    public delegate void TooltipHandler(Rect rect, Func<string> textFunc, float delay);
    public static TooltipHandler ActiveTooltipHandler { get; set; }

    public void Tooltip(string text, float delay = 0.3f)
    {
        Tooltip(() => text, delay);
    }

    public void Tooltip(Func<string> textFunc, float delay = 0.3f)
    {
        if (Event.current.type == EventType.Repaint)
        {
            ActiveTooltipHandler?.Invoke(GUILayoutUtility.GetLastRect(), textFunc, delay);
        }
    }
    
    protected ISupplier<double> SupplierOrFixed(ValueConnectionKnob input, double fixedValue)
    {
        return SupplierOrFallback(input, Supplier.Of(fixedValue));
    }

    protected ISupplier<double> SupplierOrFallback(ValueConnectionKnob input, ISupplier<double> fixedValue)
    {
        return input != null && input.connected() ? input.GetValue<ISupplier<double>>() : fixedValue;
    }
    
    protected ISupplier<GridFunction> SupplierOrFixed(ValueConnectionKnob input, GridFunction fixedValue)
    {
        ISupplier<GridFunction> val = null;
        if (input.connected()) val = input.GetValue<ISupplier<GridFunction>>();
        if (input == null) val = Supplier.Of(fixedValue);
        return val;
    }

    protected void RefreshIfConnected(ValueConnectionKnob input, ref double value)
    {
        if (input != null && input.connected()) value = input.GetValue<ISupplier<double>>()?.ResetAndGet() ?? 0f;
    }
}
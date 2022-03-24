using System;
using System.Collections.Generic;
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
    
    public override Vector2 MinSize => new(200, 10);
    public override Vector2 DefaultSize => MinSize;
    public override bool AutoLayout => true;

    public int RandSeed = SeedSource.Next();
    public int CombinedSeed => RandSeed ^ TerrainCanvas.RandSeed;

    public const int FirstKnobPosition = 37;
    
    public GUILayoutOption[] BoxLayout => new[] { GUILayout.Width(DefaultSize.x / 2f - 13f), GUILayout.Height(20f) };
    public GUILayoutOption[] DoubleBoxLayout => new[] { GUILayout.Width(DefaultSize.x - 12f), GUILayout.Height(20f) };

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
    
    public override void OnCreate(bool fromGUI)
    {
        RefreshDynamicKnobs();
        if (fromGUI) PrepareGUI();
    }

    public virtual void PrepareGUI() {}
    
    public virtual void CleanUpGUI() {}

    public virtual void RefreshPreview() {}
    
    public virtual void RefreshDynamicKnobs() {}

    protected void KnobValueField(ValueConnectionKnob knob, ref double value, string label = null)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label ?? knob.name, BoxLayout);
        
        GUILayout.FlexibleSpace();

        if (knob.connected())
        {
            GUI.enabled = false;
            RTEditorGUI.FloatField(GUIContent.none, (float) Math.Round(value, 2), BoxLayout);
            GUI.enabled = true;
        }
        else
        {
            value = RTEditorGUI.FloatField(GUIContent.none, (float) value, BoxLayout);
        }

        GUILayout.EndHorizontal();
        
        knob.SetPosition();
    }
    
    protected void ValueField(string label, ref double value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, BoxLayout);
        
        GUILayout.FlexibleSpace();
        
        value = RTEditorGUI.FloatField(GUIContent.none, (float) value, BoxLayout);
        
        GUILayout.EndHorizontal();
    }
    
    protected void IntField(string label, ref int value)
    {
        GUILayout.BeginHorizontal(BoxStyle);
        
        GUILayout.Label(label, BoxLayout);
        
        GUILayout.FlexibleSpace();
        
        value = RTEditorGUI.IntField(GUIContent.none, value, BoxLayout);
        
        GUILayout.EndHorizontal();
    }
    
    public delegate void DropdownHandler(List<string> potentialValues, Action<int> action);
    public static DropdownHandler ActiveDropdownHandler { get; set; }

    public static void Dropdown<T>(List<T> potentialValues, Action<T> action, Func<T, string> textFunc = null)
    {
        ActiveDropdownHandler?.Invoke(potentialValues.Select(e => textFunc != null ? textFunc.Invoke(e) : e?.ToString() ?? "None").ToList(), i =>
        {
            action(potentialValues[i]);
        });
    }

    public static void Dropdown<T>(Action<T> action) where T : Enum
    {
        Dropdown(typeof(T).GetEnumValues().Cast<T>().ToList(), action, e => e.ToString().Replace('_', ' '));
    }
    
    public static void SelectionMenu<T>(GenericMenu menu, List<T> potentialValues, Action<T> action, Func<T, string> textFunc = null)
    {
        foreach (T value in potentialValues)
        {
            string text = textFunc != null ? textFunc.Invoke(value) : value.ToString();
            menu.AddItem(new GUIContent(text), false, () => action(value));
        }
    }

    public static void SelectionMenu<T>(GenericMenu menu, Action<T> action, string prefix = "") where T : Enum
    {
        SelectionMenu(menu, typeof(T).GetEnumValues().Cast<T>().ToList(), action, e => prefix + e.ToString().Replace('_', ' '));
    }
    
    public delegate void TooltipHandler(Rect rect, Func<string> textFunc, float delay);
    public static TooltipHandler ActiveTooltipHandler { get; set; }

    public static void Tooltip(string text, float delay = 0.3f)
    {
        Tooltip(() => text, delay);
    }

    public static void Tooltip(Func<string> textFunc, float delay = 0.3f)
    {
        if (Event.current.type == EventType.Repaint)
        {
            ActiveTooltipHandler?.Invoke(GUILayoutUtility.GetLastRect(), textFunc, delay);
        }
    }
    
    protected ISupplier<double> SupplierOrValueFixed(ValueConnectionKnob input, double fixedValue)
    {
        return SupplierOrFallback(input, Supplier.Of(fixedValue));
    }

    protected ISupplier<double> SupplierOrFallback(ValueConnectionKnob input, ISupplier<double> fixedValue)
    {
        return input != null && input.connected() ? input.GetValue<ISupplier<double>>() : fixedValue;
    }

    protected ISupplier<IGridFunction<T>> SupplierOrGridFixed<T>(ValueConnectionKnob input, IGridFunction<T> fixedValue)
    {
        return SupplierOrFixed(input, fixedValue);
    }

    protected ISupplier<T> SupplierOrFixed<T>(ValueConnectionKnob input, T fixedValue)
    {
        ISupplier<T> val = null;
        if (input.connected()) val = input.GetValue<ISupplier<T>>();
        return val ?? Supplier.Of(fixedValue);
    }

    protected void RefreshIfConnectedX(ValueConnectionKnob input, ref double value)
    {
        if (input != null && input.connected()) value = input.GetValue<ISupplier<double>>()?.ResetAndGet() ?? 0f;
    }
    
    protected T RefreshIfConnectedX<T>(ValueConnectionKnob input, T value)
    {
        if (input == null || !input.connected()) return value;
        ISupplier<T> supplier = input.GetValue<ISupplier<T>>();
        if (supplier == null) return value;
        return supplier.ResetAndGet();
    }
    
    protected ISupplier<T> GetIfConnected<T>(ValueConnectionKnob input)
    {
        if (input == null || !input.connected()) return null;
        return input.GetValue<ISupplier<T>>();
    }
    
    protected ValueConnectionKnob FindDynamicKnob(ConnectionKnobAttribute attribute)
    {
        return (ValueConnectionKnob) dynamicConnectionPorts.FirstOrDefault(k => k.name.Equals(attribute.Name));
    }
}
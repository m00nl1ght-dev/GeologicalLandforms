using System;
using System.Linq;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class DefFunctionConnection<T> : ValueConnectionType where T : Def
{
    public override Type Type => typeof(ISupplier<T>);

    public void SelectorUI(NodeBase node, string current, bool enabled, Action<T> onSelected, GUILayoutOption[] layout = null)
    {
        GUI.enabled = enabled;

        layout ??= node.BoxLayout;

        if (GUILayout.Button(DisplayName(FromString(current)), GUI.skin.box, layout))
        {
            NodeBase.Dropdown(new[] { (T) null }.Concat(DefDatabase<T>.AllDefsListForReading).ToList(), onSelected, DisplayName);
        }

        GUI.enabled = true;
    }

    public string DisplayName(T def)
    {
        return def == null ? "None" : def.label.CapitalizeFirst();
    }

    public string ToString(T def)
    {
        return def == null ? "None" : def.defName;
    }

    public T FromString(string defName)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? null
            : DefDatabase<T>.GetNamed(defName, false);
    }
}

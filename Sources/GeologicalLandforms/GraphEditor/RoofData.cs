using System;
using System.Linq;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public readonly struct RoofData
{
    public static readonly RoofData Empty = new();
    public static readonly IGridFunction<RoofData> EmptyGrid = GridFunction.Of(Empty);

    public readonly RoofDef Roof;
    public readonly int SelectionIndex;

    public bool IsEmpty => Roof == null;
    public bool HasRoof => Roof != null;

    public RoofData(RoofDef roof, int selectionIndex = 1)
    {
        Roof = roof;
        SelectionIndex = selectionIndex;
    }

    public static void RoofSelector(NodeBase node, string current, bool enabled, Action<RoofDef> onSelected)
    {
        GUI.enabled = enabled;

        if (GUILayout.Button(DislayString(current), GUI.skin.box, node.BoxLayout))
        {
            NodeBase.Dropdown(new[] { (RoofDef) null }.Concat(DefDatabase<RoofDef>.AllDefsListForReading).ToList(), onSelected, DislayString);
        }

        GUI.enabled = true;
    }

    public static string DislayString(string defName)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? "None"
            : DefDatabase<RoofDef>.GetNamed(defName, false)?.label.CapitalizeFirst() ?? "None";
    }

    public static string DislayString(RoofDef def)
    {
        return def == null ? "None" : def.label.CapitalizeFirst();
    }

    public override string ToString()
    {
        return IsEmpty ? "None" : Roof.defName;
    }

    public static string ToString(RoofDef def)
    {
        return def == null ? "None" : def.defName;
    }

    public static RoofData FromString(string defName, RoofDef fallback = null)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? new RoofData(fallback)
            : new RoofData(DefDatabase<RoofDef>.GetNamed(defName, false));
    }
}

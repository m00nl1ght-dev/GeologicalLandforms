using System;
using System.Linq;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public readonly struct TerrainData
{
    public static readonly TerrainData Empty = new();
    public static readonly IGridFunction<TerrainData> EmptyGrid = GridFunction.Of(Empty);

    public readonly TerrainDef Terrain;
    public readonly int SelectionIndex;

    public bool IsEmpty => Terrain == null;
    public bool HasTerrain => Terrain != null;

    public TerrainData(TerrainDef terrain, int selectionIndex = 1)
    {
        Terrain = terrain;
        SelectionIndex = selectionIndex;
    }

    public static void TerrainSelector(NodeBase node, string current, bool enabled, Action<TerrainDef> onSelected, GUILayoutOption[] layout = null)
    {
        GUI.enabled = enabled;

        layout ??= node.BoxLayout;

        if (GUILayout.Button(DislayString(current), GUI.skin.box, layout))
        {
            NodeBase.Dropdown(new[] { (TerrainDef) null }.Concat(DefDatabase<TerrainDef>.AllDefsListForReading).ToList(), onSelected, DislayString);
        }

        GUI.enabled = true;
    }

    public static string DislayString(string defName)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? "None"
            : DefDatabase<TerrainDef>.GetNamed(defName, false)?.label.CapitalizeFirst() ?? "None";
    }

    public static string DislayString(TerrainDef def)
    {
        return def == null ? "None" : def.label.CapitalizeFirst();
    }

    public override string ToString()
    {
        return IsEmpty ? "None" : Terrain.defName;
    }

    public static string ToString(TerrainDef def)
    {
        return def == null ? "None" : def.defName;
    }

    public static TerrainData FromString(string defName)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? new TerrainData()
            : new TerrainData(DefDatabase<TerrainDef>.GetNamed(defName, false));
    }
}

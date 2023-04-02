using System;
using System.Linq;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public readonly struct BiomeData
{
    public static readonly BiomeData Empty = new();
    public static readonly IGridFunction<BiomeData> EmptyGrid = GridFunction.Of(Empty);

    public readonly BiomeDef Biome;
    public readonly int SelectionIndex;

    public bool IsEmpty => Biome == null;
    public bool HasBiome => Biome != null;

    public BiomeData(BiomeDef biome, int selectionIndex = 1)
    {
        Biome = biome;
        SelectionIndex = selectionIndex;
    }

    public static void BiomeSelector(NodeBase node, string current, bool enabled, Action<BiomeDef> onSelected, GUILayoutOption[] layout = null)
    {
        GUI.enabled = enabled;

        layout ??= node.BoxLayout;

        if (GUILayout.Button(DislayString(current), GUI.skin.box, layout))
        {
            NodeBase.Dropdown(new[] { (BiomeDef) null }.Concat(DefDatabase<BiomeDef>.AllDefsListForReading).ToList(), onSelected, DislayString);
        }

        GUI.enabled = true;
    }

    public static string DislayString(string defName)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? "None"
            : DefDatabase<BiomeDef>.GetNamed(defName, false)?.label.CapitalizeFirst() ?? "None";
    }

    public static string DislayString(BiomeDef def)
    {
        return def == null ? "None" : def.label.CapitalizeFirst();
    }

    public override string ToString()
    {
        return IsEmpty ? "None" : Biome.defName;
    }

    public static string ToString(BiomeDef def)
    {
        return def == null ? "None" : def.defName;
    }

    public static BiomeData FromString(string defName, BiomeDef fallback = null)
    {
        return string.IsNullOrEmpty(defName) || defName.EqualsIgnoreCase("None") ? new BiomeData(fallback)
            : new BiomeData(DefDatabase<BiomeDef>.GetNamed(defName, false));
    }
}

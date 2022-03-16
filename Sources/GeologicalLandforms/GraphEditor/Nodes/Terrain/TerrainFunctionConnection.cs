using System;
using System.Linq;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace TerrainGraph;

public class TerrainFunctionConnection : ValueConnectionType
{
    public const string Id = "TerrainFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(1.5f, 0f, 0f);
    public override Type Type => typeof(ISupplier<TerrainDef>);

    public static void TerrainSelector(NodeBase node, string current, bool enabled, Action<TerrainDef> onSelected)
    {
        GUI.enabled = enabled;
        
        if (GUILayout.Button(DislayString(current), GUI.skin.box, node.BoxLayout))
        {
            NodeBase.Dropdown(new[] { (TerrainDef) null }.Concat(DefDatabase<TerrainDef>.AllDefsListForReading).ToList(), onSelected, e => DislayString(ToString(e)));
        }

        GUI.enabled = true;
    }
    
    public static string DislayString(string str)
    {
        return string.IsNullOrEmpty(str) ? "None" : str;
    }

    public static string ToString(TerrainDef def)
    {
        return def == null ? "" : def.defName;
    }
    
    public static TerrainDef FromString(string defName)
    {
        return string.IsNullOrEmpty(defName) ? null : TerrainDef.Named(defName);
    }
}
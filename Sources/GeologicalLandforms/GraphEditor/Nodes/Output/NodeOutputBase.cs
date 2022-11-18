using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
public abstract class NodeOutputBase : NodeBase
{
    public Landform Landform => (Landform) canvas;

    public virtual ValueConnectionKnob InputKnobRef => null;
    public virtual ValueConnectionKnob OutputKnobRef => null;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(InputKnobRef.name, BoxLayout);
        GUILayout.EndHorizontal();
        InputKnobRef.SetPosition();
        OutputKnobRef?.SetPosition();

        GUILayout.EndVertical();
    }
}
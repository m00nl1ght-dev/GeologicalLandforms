using System;
using System.Linq;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Other/Apply Layer", 9001)]
public class NodeApplyLayer : NodeBase
{
    public const string ID = "applyLayer";
    public override string GetID => ID;

    public override string Title => "Apply Layer";

    public Landform Landform => (Landform) canvas;

    public string LayerId = "";

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        GUILayout.BeginHorizontal(BoxStyle);

        if (GUILayout.Button(LayerId, GUI.skin.box, FullBoxLayout))
        {
            var options = LandformManager.LandformsById.Values.Where(lf => lf.IsLayer && lf.WorldTileReq == null);
            Dropdown(options.ToList(), OnLandformSelected, t => t.Id);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    private void OnLandformSelected(Landform landform)
    {
        LayerId = landform.Id;
        canvas.OnNodeChange(this);
    }

    public override void OnCreate(bool fromGUI)
    {
        Landform.ApplyLayers.AddDistinct(this);
    }

    protected override void OnDelete()
    {
        Landform.ApplyLayers.Remove(this);
    }
}

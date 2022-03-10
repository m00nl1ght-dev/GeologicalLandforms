using System;
using GeologicalLandforms.TerrainGraph.Nodes;
using NodeEditorFramework;
using NodeEditorFramework.Standard;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[NodeCanvasType("Landform")]
public class Landform : NodeCanvas
{
    public string Id => Manifest?.Id;
    public bool IsCustom => Manifest?.IsCustom ?? false;

    public string DisplayName => Manifest.DisplayName ?? "";
    public bool DisplayNameHasDirection => Manifest?.DisplayNameHasDirection ?? false;

    public NodeLandformManifest Manifest { get; internal set; }
    public NodeWorldTileReq WorldTileReq { get; internal set; }

    public override string canvasName => Id ?? "Landform";
    public Vector2 ScreenOrigin = new(- Screen.width / 2f, - Screen.height / 2f + LandformGraphInterface.toolbarHeight);

    public string TranslatedName => DisplayName?.Length > 0 ? DisplayName : Id != null ? ("GeologicalLandforms.Landform." + Id).Translate() : "Unknown";
    public string TranslatedNameForSelection => TranslatedName + (IsCornerVariant ? (" " + "GeologicalLandforms.Landform.Corner".Translate()) : "");
    public bool IsCornerVariant => WorldTileReq?.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides;

    protected override void OnCreate() 
    {
        ValidateSelf();
    }

    protected override void ValidateSelf()
    {
        Traversal ??= new LandformCalculator(this);
        if (Manifest == null)
        {
            Node node = Node.Create(NodeLandformManifest.ID, new Vector2(), this);
        }
        if (WorldTileReq == null) Node.Create(NodeWorldTileReq.ID, new Vector2(), this);
    }

    public override bool CanAddNode(string nodeID)
    {
        return nodeID switch
        {
            NodeLandformManifest.ID => true,
            NodeWorldTileReq.ID => true,
            _ => Id != null
        };
    }
    
    public override bool CanDeleteNode(Node node)
    {
        if (node == Manifest) return false;
        if (node == WorldTileReq) return false;
        if (Id == null) return false;
        return true;
    }

    public void ResetView()
    {
        ValidateSelf();
        Manifest.position = ScreenOrigin + new Vector2(10f, 3f);
        WorldTileReq.position = new Vector2(ScreenOrigin.x + 10f, (IsCustom ? Manifest.rect.yMax : ScreenOrigin.y) + 10f);
    }
}
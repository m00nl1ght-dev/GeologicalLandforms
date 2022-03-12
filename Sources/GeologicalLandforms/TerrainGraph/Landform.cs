using System;
using NodeEditorFramework;
using NodeEditorFramework.Standard;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[NodeCanvasType("Landform")]
public class Landform : NodeCanvas
{
    public static WorldTileInfo GeneratingWorldTile { get; private set; }
    public static Landform GeneratingLandform { get; private set; }
    
    public string Id => Manifest?.Id;
    public bool IsCustom => Manifest?.IsCustom ?? false;

    public string DisplayName => Manifest.DisplayName ?? "";
    public bool DisplayNameHasDirection => Manifest?.DisplayNameHasDirection ?? false;

    public NodeUILandformManifest Manifest { get; internal set; }
    public NodeUIWorldTileReq WorldTileReq { get; internal set; }

    public override string canvasName => Id ?? "Landform";
    public Vector2 ScreenOrigin = new(- Screen.width / 2f, - Screen.height / 2f + LandformGraphInterface.toolbarHeight);

    public string TranslatedName => DisplayName?.Length > 0 ? DisplayName : Id != null ? ("GeologicalLandforms.Landform." + Id).Translate() : "Unknown";
    public string TranslatedNameForSelection => TranslatedName + (IsCornerVariant ? (" " + "GeologicalLandforms.Landform.Corner".Translate()) : "");
    public bool IsCornerVariant => WorldTileReq?.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides;
    
    

    public static void PrepareMapGen(Map map)
    {
        CleanUpMapGen();
        GeneratingWorldTile = WorldTileInfo.GetWorldTileInfo(map.Tile);
        
        if (!(GeneratingWorldTile?.LandformId?.Length > 0)) return;
        if (!LandformManager.Landforms.TryGetValue(GeneratingWorldTile.LandformId, out Landform landform)) return;
        
        int mapSize = Math.Min(map.Size.x, map.Size.z);
        if (!landform.WorldTileReq.MapSizeRequirement.Includes(mapSize)) return;
        
        GeneratingLandform = landform;
    }
    
    public static void CleanUpMapGen()
    {
        GeneratingLandform = null;
        GeneratingWorldTile = null;
    }

    protected override void OnCreate() 
    {
        ValidateSelf();
    }

    protected override void ValidateSelf()
    {
        Traversal ??= new LandformCalculator(this);
        if (Manifest == null) Node.Create(NodeUILandformManifest.ID, new Vector2(), this);
        if (WorldTileReq == null) Node.Create(NodeUIWorldTileReq.ID, new Vector2(), this);
    }

    public override bool CanAddNode(string nodeID, bool isEditorAction)
    {
        return nodeID switch
        {
            NodeUILandformManifest.ID => !isEditorAction,
            NodeUIWorldTileReq.ID => !isEditorAction,
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
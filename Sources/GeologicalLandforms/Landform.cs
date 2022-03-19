using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[NodeCanvasType("Landform")]
public class Landform : TerrainCanvas
{
    public static IWorldTileInfo GeneratingTile { get; private set; }
    public static Landform GeneratingLandform => GeneratingTile?.Landform;
    public static bool IsAnyGenerating => GeneratingTile?.Landform != null;
    public static int GeneratingMapSize { get; private set; } = 250;

    public string Id => Manifest?.Id;
    public bool IsCustom => Manifest?.IsCustom ?? false;

    public string DisplayName => Manifest.DisplayName ?? "";
    public bool DisplayNameHasDirection => Manifest?.DisplayNameHasDirection ?? false;

    public NodeUILandformManifest Manifest { get; internal set; }
    public NodeUIWorldTileReq WorldTileReq { get; internal set; }
    
    public NodeOutputElevation OutputElevation { get; internal set; }
    public NodeOutputFertility OutputFertility { get; internal set; }

    public override int GridFullSize => 250;
    public override int GridPreviewSize => 100;

    public override string canvasName => Id ?? "Landform";
    public Vector2 ScreenOrigin = new(- Screen.width / 2f, - Screen.height / 2f + LandformGraphInterface.ToolbarHeight);

    public string TranslatedName => DisplayName?.Length > 0 ? DisplayName : Id != null ? ("GeologicalLandforms.Landform." + Id).Translate() : "Unknown";
    public string TranslatedNameForSelection => TranslatedName + (IsCornerVariant ? (" " + "GeologicalLandforms.Landform.Corner".Translate()) : "");
    public bool IsCornerVariant => WorldTileReq?.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides;

    public static void PrepareMapGen(Map map)
    {
        CleanUp();
        GeneratingTile = WorldTileInfo.GetWorldTileInfo(map.Tile);
        GeneratingMapSize = Math.Min(map.Size.x, map.Size.z);

        if (GeneratingTile.Landform == null) return;
        if (!GeneratingTile.Landform.WorldTileReq.CheckMapRequirements(map)) return;
        
        GeneratingLandform.RandSeed = NodeBase.SeedSource.Next();
        GeneratingLandform.TraverseAll();
    }

    public static void PrepareEditor(EditorMockTileInfo tileInfo)
    {
        CleanUp();
        GeneratingTile = tileInfo;
        GeneratingMapSize = 250;
        GeneratingLandform.RandSeed = NodeBase.SeedSource.Next();
    }

    public static void CleanUp()
    {
        GeneratingTile = null;
    }

    protected override void ValidateSelf()
    {
        base.ValidateSelf();
        if (Manifest == null) Node.Create(NodeUILandformManifest.ID, new Vector2(), this);
        if (WorldTileReq == null) Node.Create(NodeUIWorldTileReq.ID, new Vector2(), this);
    }

    public override bool CanAddNode(string nodeID, bool isEditorAction)
    {
        return nodeID switch
        {
            NodeUILandformManifest.ID => !isEditorAction,
            NodeUIWorldTileReq.ID => !isEditorAction,
            _ => Id != null || !isEditorAction
        };
    }
    
    public override bool CanDeleteNode(Node node)
    {
        if (node == Manifest) return false;
        if (node == WorldTileReq) return false;
        if (Id == null) return false;
        return true;
    }

    public override bool CanOpenContextMenu(ContextType type)
    {
        return Id != null;
    }

    public override void ResetView()
    {
        base.ResetView();
        Manifest.position = ScreenOrigin + new Vector2(10f, 3f);
        WorldTileReq.position = new Vector2(ScreenOrigin.x + 10f, (IsCustom ? Manifest.rect.yMax : ScreenOrigin.y) + 10f);
    }
}
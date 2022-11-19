using System;
using System.Collections.Generic;
using System.Linq;
using MapPreview;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[NodeCanvasType("Landform")]
public class Landform : TerrainCanvas
{
    public const int DefaultGridFullSize = 250;
    public const int DefaultGridPreviewSize = 100;

    public static int GeneratingGridFullSize { get; private set; } = DefaultGridFullSize;
    public static int GeneratingGridPreviewSize { get; private set; } = DefaultGridPreviewSize;
    
    public static IWorldTileInfo GeneratingTile { get; private set; }
    public static IReadOnlyList<Landform> GeneratingLandforms { get; private set; }
    
    public static bool AnyGenerating => GeneratingLandforms is { Count: > 0 };

    public static IntVec2 GeneratingMapSize { get; set; } = new(250, 250);
    public static int GeneratingSeed { get; private set; }

    public static int GeneratingMapSizeMin => Math.Min(GeneratingMapSize.x, GeneratingMapSize.z);
    public static double MapSpaceToNodeSpaceFactor => GeneratingMapSizeMin / (double) GeneratingGridFullSize;
    public static double NodeSpaceToMapSpaceFactor => GeneratingGridFullSize / (double) GeneratingMapSizeMin;

    public string Id => Manifest?.Id;
    public bool IsCustom => Manifest?.IsCustom ?? false;
    public bool IsLayer => LayerConfig != null;
    public int Priority => IsLayer ? LayerConfig.Priority : 0;

    public string DisplayName => Manifest.DisplayName ?? "";
    public bool DisplayNameHasDirection => Manifest?.DisplayNameHasDirection ?? false;

    public NodeUILandformManifest Manifest { get; internal set; }
    public NodeUIWorldTileReq WorldTileReq { get; internal set; }
    public NodeUILayerConfig LayerConfig { get; internal set; }
    
    public NodeInputElevation InputElevation { get; internal set; }
    public NodeInputFertility InputFertility { get; internal set; }
    public NodeInputBiomeGrid InputBiomeGrid { get; internal set; }
    public NodeInputCaves InputCaves { get; internal set; }
    
    public NodeOutputElevation OutputElevation { get; internal set; }
    public NodeOutputFertility OutputFertility { get; internal set; }
    public NodeOutputTerrain OutputTerrain { get; internal set; }
    public NodeOutputBiomeGrid OutputBiomeGrid { get; internal set; }
    public NodeOutputCaves OutputCaves { get; internal set; }
    public NodeOutputScatterers OutputScatterers { get; internal set; }

    public override int GridFullSize => GeneratingGridFullSize;
    public override int GridPreviewSize => GeneratingGridPreviewSize;

    public override string canvasName => Id ?? "Landform";
    public Vector2 ScreenOrigin = new(-960f, -540f + LandformGraphInterface.ToolbarHeight);
    
    public bool IsCornerVariant => WorldTileReq?.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides;

    public static void PrepareMapGen(Map map)
    {
        LandformGraphEditor.ActiveEditor?.Close();
        
        var world = Find.World;
        bool rerolled = SeedRerollData.IsMapSeedRerolled(world, map.Tile, out var savedSeed);
        int seed = rerolled ? savedSeed : world.info.Seed ^ map.Tile;
        
        PrepareMapGen(new IntVec2(map.Size.x, map.Size.z), map.Tile, seed);
    }
    
    public static void PrepareMapGen(IntVec2 mapSize, int worldTile, int seed)
    {
        CleanUp();
        GeneratingTile = WorldTileInfo.Get(worldTile);
        GeneratingGridFullSize = GeologicalLandformsAPI.LandformGridSizeFunction.Invoke();
        GeneratingMapSize = mapSize;
        GeneratingSeed = seed;

        if (GeneratingTile.Landforms == null) return;
        
        var landformStack = new List<Landform>();
        GeneratingLandforms = landformStack;
        
        foreach (var landform in GeneratingTile.Landforms.Where(l => l.WorldTileReq.CheckMapRequirements(mapSize)))
        {
            landform.RandSeed = seed;
            landform.TraverseAll();
            landformStack.Add(landform);
        }
    }

    public static void PrepareEditor(EditorMockTileInfo tileInfo)
    {
        CleanUp();
        GeneratingTile = tileInfo;
        GeneratingGridFullSize = GeologicalLandformsAPI.LandformGridSizeFunction.Invoke();
        GeneratingMapSize = new IntVec2(250, 250);
        GeneratingSeed = NodeBase.SeedSource.Next();
        if (GeneratingTile.Landforms == null) return;
        GeneratingLandforms = GeneratingTile.Landforms;
        foreach (var landform in GeneratingLandforms) landform.RandSeed = GeneratingSeed;
    }

    public static void CleanUp()
    {
        GeneratingTile = null;
        GeneratingLandforms = null;
    }
    
    public static T GetFeature<T>(Func<Landform, T> func)
    {
        if (GeneratingLandforms == null) return default;
        return GeneratingLandforms.Select(func).LastOrDefault(v => v != null);
    }
    
    public static IGridFunction<T> GetFeatureScaled<T>(Func<Landform, IGridFunction<T>> func)
    {
        var gridFunction = GetFeature(func);
        return gridFunction == null ? null : TransformIntoMapSpace(gridFunction);
    }

    public static IGridFunction<T> TransformIntoMapSpace<T>(IGridFunction<T> gridInNodeSpace)
    {
        return new GridFunction.Transform<T>(gridInNodeSpace, NodeSpaceToMapSpaceFactor);
    }
    
    public static IGridFunction<T> TransformIntoNodeSpace<T>(IGridFunction<T> gridInMapSpace)
    {
        return new GridFunction.Transform<T>(gridInMapSpace, MapSpaceToNodeSpaceFactor);
    }

    protected override void ValidateSelf()
    {
        base.ValidateSelf();
        if (Manifest == null) Node.Create(NodeUILandformManifest.ID, new Vector2(), this);
        if (WorldTileReq == null) Node.Create(NodeUIWorldTileReq.ID, new Vector2(), this);
    }

    public override void OnNodeChange(Node node)
    {
        base.OnNodeChange(node);
        if (Manifest != null) Manifest.IsEdited = true;
    }

    public override bool CanAddNode(string nodeID, bool isEditorAction)
    {
        return nodeID switch
        {
            NodeUILandformManifest.ID => !isEditorAction,
            NodeUIWorldTileReq.ID => !isEditorAction,
            NodeUILayerConfig.ID => !isEditorAction || IsCustom,
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
        if (Manifest != null) Manifest.position = ScreenOrigin + new Vector2(10f, 3f);
        if (WorldTileReq != null) WorldTileReq.position = new Vector2(ScreenOrigin.x + 10f, (IsCustom ? Manifest.rect.yMax + 10f : ScreenOrigin.y + 3f));
    }
    
    public string TranslatedName => 
        DisplayName?.Length > 0 ? DisplayName : 
        Id == null ? "Unknown" : 
        IsCustom ? Id : 
        ("GeologicalLandforms.Landform." + Id).Translate();
    
    public string TranslatedNameForSelection => 
        TranslatedName + (IsCornerVariant ? (" " + "GeologicalLandforms.Landform.Corner".Translate()) : "");
    
    public string TranslatedNameWithDirection(Rot4 direction)
    {
        if (!DisplayNameHasDirection) return TranslatedName;
        return TranslatedDirection(direction) + " " + TranslatedName;
    }
    
    public string TranslatedDirection(Rot4 direction)
    {
        if (!IsCornerVariant) return TranslateRot4(direction);
        return TranslateDoubleRot4(direction);
    }

    private static string TranslateRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4." + rot4.AsInt).Translate();
    }
    
    private static string TranslateDoubleRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4.Double." + rot4.AsInt).Translate();
    }

    public bool IsPointOfInterest()
    {
        var topology = WorldTileReq.Topology;
        var commonness = WorldTileReq.Commonness;
        if (topology == Topology.Any) return commonness < 0.1f;
        if (topology.IsCommon()) return commonness < 0.5f;
        return true;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using MapPreview;
using NodeEditorFramework;
using TerrainGraph;
using TerrainGraph.Util;
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
    public static bool AnyGeneratingNonLayer => AnyGenerating && GeneratingLandforms.Any(lf => !lf.IsLayer);

    public static IntVec2 GeneratingMapSize { get; set; } = new(250, 250);

    public static int GeneratingMapSizeMin => Math.Min(GeneratingMapSize.x, GeneratingMapSize.z);
    public static double MapSpaceToNodeSpaceFactor => GeneratingMapSizeMin / (double) GeneratingGridFullSize;
    public static double NodeSpaceToMapSpaceFactor => GeneratingGridFullSize / (double) GeneratingMapSizeMin;

    private static readonly Dictionary<(Type, string), object> NamedFeatureCache = [];

    public string Id => Manifest?.Id;
    public int IdHash => GenText.StableStringHash(Id ?? "");
    public bool IsCustom => Manifest?.IsCustom ?? false;
    public bool IsInternal => Manifest?.IsInternal ?? false;
    public bool IsEdited => Manifest?.IsEdited ?? false;
    public bool IsLayer => LayerConfig != null;
    public int Priority => IsLayer ? LayerConfig.Priority : 0;

    public string DisplayName => Manifest.DisplayName ?? "";
    public bool DisplayNameHasDirection => Manifest?.DisplayNameHasDirection ?? false;

    public string OriginalFileLocation { get; internal set; }
    public ModContentPack ModContentPack { get; internal set; }

    public NodeUILandformManifest Manifest { get; internal set; }
    public NodeUIWorldTileReq WorldTileReq { get; internal set; }
    public NodeUIWorldTileGraphic WorldTileGraphic { get; internal set; }
    public NodeUIMapIncidents MapIncidents { get; internal set; }
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
    public NodeOutputRoofGrid OutputRoofGrid { get; internal set; }
    public NodeOutputScatterers OutputScatterers { get; internal set; }
    public NodeOutputTerrainPatches OutputTerrainPatches { get; internal set; }
    public NodeOutputWaterFlow OutputWaterFlow { get; internal set; }

    public List<NodeRunGenStep> CustomGenSteps { get; } = [];

    public override int GridFullSize => GeneratingGridFullSize;
    public override int GridPreviewSize => GeneratingGridPreviewSize;

    public override string canvasName => Id ?? "Landform";
    public Vector2 ScreenOrigin = new(-960f, -540f + LandformGraphInterface.ToolbarHeight);

    public override IPreviewScheduler PreviewScheduler => LandformPreviewScheduler.Instance;

    public bool IsCornerVariant => WorldTileReq?.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides;

    public static void Prepare(Map map)
    {
        var tileInfo = WorldTileInfo.Get(map, false);
        var landformSeed = map.Tile >= 0 ? SeedRerollData.GetMapSeed(Find.World, map.Tile) : Rand.Int;
        Prepare(tileInfo, new IntVec2(map.Size.x, map.Size.z), landformSeed);
    }

    public static void Prepare(IWorldTileInfo tileInfo, IntVec2 mapSize, int seed)
    {
        CleanUp();

        GeneratingTile = tileInfo;
        GeneratingMapSize = mapSize;
        GeneratingGridFullSize = GeologicalLandformsAPI.LandformGridSize.Invoke();

        var landforms = GeneratingTile.Landforms;
        if (landforms == null) return;

        var landformStack = new List<Landform>();
        GeneratingLandforms = landformStack;

        foreach (var landform in landforms)
        {
            if (landform.WorldTileReq == null || landform.WorldTileReq.CheckWorldObject(tileInfo))
            {
                landform.RandSeed = seed;
                landform.ClearNamedInputs();
                landform.SetNamedInputs(landformStack);
                landform.TraverseAll();
                landformStack.Add(landform);
            }
        }
    }

    public static void CleanUp()
    {
        GeneratingTile = null;
        GeneratingLandforms = null;
        NamedFeatureCache.Clear();
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

    public static T GetNamedFeature<T>(string name)
    {
        if (NamedFeatureCache.TryGetValue((typeof(T), name), out var cached)) return (T) cached;
        var supplier = GetFeature(lf => lf.GetNamedOutput<T>(name));
        var value = supplier != null ? supplier.ResetAndGet() : default;
        NamedFeatureCache[(typeof(T), name)] = value;
        return value;
    }

    public static IGridFunction<T> TransformIntoMapSpace<T>(IGridFunction<T> gridInNodeSpace)
    {
        return new GridFunction.Transform<T>(gridInNodeSpace, NodeSpaceToMapSpaceFactor);
    }

    public static IGridFunction<T> TransformIntoNodeSpace<T>(IGridFunction<T> gridInMapSpace)
    {
        return new GridFunction.Transform<T>(gridInMapSpace, MapSpaceToNodeSpaceFactor);
    }

    public float GetCommonnessForTile(IWorldTileInfo tile, bool lenient = false)
    {
        return WorldTileReq == null ? 0f : WorldTileReq.GetCommonnessForTile(tile, lenient);
    }

    protected override void ValidateSelf()
    {
        base.ValidateSelf();
        if (Manifest == null) Node.Create(NodeUILandformManifest.ID, new Vector2(), this);
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
            NodeUIWorldTileReq.ID => !isEditorAction || WorldTileReq == null,
            NodeUILayerConfig.ID => !isEditorAction || (IsCustom && LayerConfig == null),
            _ => Id != null || !isEditorAction
        };
    }

    public override bool CanDeleteNode(Node node)
    {
        if (node == Manifest) return false;
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

    public override IRandom CreateRandomInstance()
    {
        return new MinimalRandom();
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
        if (WorldTileReq == null) return false;
        var topology = WorldTileReq.Topology;
        var commonness = WorldTileReq.Commonness;
        if (topology == Topology.Any) return commonness < 0.1f;
        if (topology.IsCommon()) return commonness < 0.5f;
        return true;
    }

    public override string ToString()
    {
        return Id;
    }

    private class MinimalRandom : IRandom
    {
        private uint _seed;
        private uint _iterations;

        public int Next() => MurmurHash.GetInt(_seed, _iterations++);

        public int Next(int min, int max) => max <= min ? min : min + Mathf.Abs(Next() % (max - min));

        public double NextDouble() => (MurmurHash.GetInt(_seed, _iterations++) - (double) int.MinValue) / uint.MaxValue;

        public double NextDouble(double min, double max) => max <= min ? min : NextDouble() * (max - min) + min;

        public void Reinitialise(int seed)
        {
            _seed = (uint) seed;
            _iterations = 0u;
        }
    }
}

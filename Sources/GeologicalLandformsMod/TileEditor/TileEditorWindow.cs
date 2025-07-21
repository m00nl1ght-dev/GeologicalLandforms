#if RW_1_6_OR_GREATER

using LunarFramework.GUI;
using LunarFramework.Patching;
using MapPreview;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TileEditor;

[StaticConstructorOnStartup]
public class TileEditorWindow : Window
{
    public static readonly Texture2D IconLoading = ContentFinder<Texture2D>.Get("LoadingIndicatorStaticGL");
    public static readonly Texture2D IconEditTile = ContentFinder<Texture2D>.Get("EditTileIconGL");

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(TileEditorWindow));

    public override Vector2 InitialSize => new(1028, 800);

    public readonly PlanetTile PlanetTile;

    private readonly LayoutRect _layout = new(GeologicalLandformsMod.LunarAPI);

    private readonly PreviewWidget _previewWidget = new(MapSizeUtility.MaxMapSize);

    public TileEditorWindow(PlanetTile planetTile)
    {
        PlanetTile = planetTile;
        forcePause = true;
        absorbInputAroundWindow = true;
        layer = WindowLayer.SubSuper;
    }

    private static readonly Color _sectionBackground = new(0.2f, 0.2f, 0.2f);
    private static readonly Color _sectionOutline = new(0.38f, 0.42f, 0.48f);

    public override void WindowOnGUI()
    {
        Widgets.DrawRectFast(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), new Color(0.0f, 0.0f, 0.0f, 0.5f));
        base.WindowOnGUI();
    }

    public override void DoWindowContents(Rect rect)
    {
        _layout.BeginRoot(rect, new LayoutParams { Horizontal = true });
        _layout.BeginAbs(400f);

        _previewWidget.Draw(_layout.Abs(400f));

        _layout.Abs(Margin);

        _layout.BeginRel(-1);
        Widgets.DrawBoxSolidWithOutline(_layout, _sectionBackground, _sectionOutline);
        DoGeneralSection();
        _layout.End();

        _layout.End();
        _layout.Abs(Margin);
        _layout.BeginRel(-1);

        _layout.BeginAbs(250f);
        Widgets.DrawBoxSolidWithOutline(_layout, _sectionBackground, _sectionOutline);
        DoFeaturesSection();
        _layout.End();

        _layout.Abs(Margin);

        _layout.BeginAbs(435f);
        Widgets.DrawBoxSolidWithOutline(_layout, _sectionBackground, _sectionOutline);
        DoFeatureConfigSection();
        _layout.End();

        _layout.Abs(Margin);

        _layout.BeginRel(-1);
        Widgets.DrawBoxSolidWithOutline(_layout, _sectionBackground, _sectionOutline);
        DoBottomRow();
        _layout.End();

        _layout.End();
        _layout.End();
    }

    private void DoGeneralSection()
    {

    }

    private void DoFeaturesSection()
    {

    }

    private void DoFeatureConfigSection()
    {

    }

    private void DoBottomRow()
    {

    }

    private void GeneratePreview()
    {
        MapPreviewGenerator.Instance.ClearQueue();

        var world = Find.World;
        int seed = SeedRerollData.GetMapSeed(world, PlanetTile);
        var mapParent = world.worldObjects.MapParentAt(PlanetTile);
        var mapSize = MapSizeUtility.DetermineMapSize(world, mapParent);

        var request = new MapPreviewRequest(seed, PlanetTile, mapSize)
        {
            TextureSize = new IntVec2(_previewWidget.Texture.width, _previewWidget.Texture.height),
            GeneratorDef = mapParent?.MapGeneratorDef ?? MapGeneratorDefOf.Base_Player,
            UseTrueTerrainColors = true,
            UseMinimalMapComponents = true,
            ExistingBuffer = _previewWidget.Buffer
        };

        MapPreviewGenerator.Instance.QueuePreviewRequest(request);
        _previewWidget.Await(request);
    }

    public override void PreOpen()
    {
        Find.WorldSelector.ClearSelection();
        CompatUtils.CloseAllMapPreviewWindows();

        base.PreOpen();

        MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);

        MapPreviewGenerator.Init();

        GeneratePreview();
    }

    public override void PreClose()
    {
        base.PreClose();

        _previewWidget.Dispose();

        MapPreviewGenerator.Instance.ClearQueue();

        MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);

        Find.WorldSelector.SelectedTile = PlanetTile;
    }

    [StaticConstructorOnStartup]
    private class PreviewWidget : MapPreviewWidget
    {
        public PreviewWidget(IntVec2 maxMapSize) : base(maxMapSize) {}

        protected override void DrawGenerated(Rect inRect)
        {
            GUI.DrawTextureWithTexCoords(inRect.ContractedBy(1f), Texture, TexCoords);
        }

        protected override void DrawGenerating(Rect inRect)
        {
            DrawPreloader(IconLoading, inRect.center);
        }
    }
}

#endif

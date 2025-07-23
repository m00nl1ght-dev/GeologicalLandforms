#if RW_1_6_OR_GREATER

using System;
using LunarFramework.GUI;
using LunarFramework.Patching;
using LunarFramework.Utility;
using MapPreview;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GeologicalLandforms.TileEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public class TileEditorWindow : Window
{
    public static readonly Texture2D IconLoading = ContentFinder<Texture2D>.Get("LoadingIndicatorStaticGL");
    public static readonly Texture2D IconEditTile = ContentFinder<Texture2D>.Get("EditTileIconGL");
    public static readonly Texture2D IconAddElement = ContentFinder<Texture2D>.Get("AddElementIconGL");
    public static readonly Texture2D TexHeaderSlant = ContentFinder<Texture2D>.Get("HeaderSlantGL");

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(TileEditorWindow));

    public override Vector2 InitialSize => new(1028, 800);

    public readonly PlanetTile PlanetTile;

    private readonly LayoutRect _layout = new(GeologicalLandformsMod.LunarAPI);

    private readonly PreviewWidget _previewWidget = new(MapSizeUtility.MaxMapSize);

    private static readonly Color _colorSectionHeader = new(0.15f, 0.15f, 0.15f);
    private static readonly Color _colorSectionBackground = new(0.2f, 0.2f, 0.2f);

    public static bool CanEditTile(PlanetTile tile)
    {
        if (!tile.Valid || !tile.Layer.IsRootSurface || !MapPreviewAPI.IsReadyForPreviewGen) return false;
        if (Find.WorldObjects.MapParentAt(tile) is { HasMap: true }) return false;
        return true;
    }

    public TileEditorWindow(PlanetTile planetTile)
    {
        PlanetTile = planetTile;
        forcePause = true;
        absorbInputAroundWindow = true;
        layer = WindowLayer.SubSuper;
    }

    public override void WindowOnGUI()
    {
        Widgets.DrawRectFast(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), new Color(0.0f, 0.0f, 0.0f, 0.5f));
        base.WindowOnGUI();
    }

    public override void DoWindowContents(Rect rect)
    {
        _layout.BeginRoot(rect, new LayoutParams { Horizontal = true });

        _layout.BeginAbs(400f);
        DoSectionFrame("Preview", 400f, () => _previewWidget.Draw(_layout));
        _layout.Abs(Margin);
        DoSectionFrame("General", 100f, DoGeneralSection, DoGeneralSectionButtons);
        _layout.Abs(Margin);
        DoSectionFrame("Stone types", -1, DoRockTypesSection, DoRockTypesSectionButtons);
        _layout.End();

        _layout.Abs(Margin);

        _layout.BeginRel(-1);
        DoSectionFrame("Features", 250f, DoFeaturesSection, DoFeaturesSectionButtons);
        _layout.Abs(Margin);
        DoSectionFrame("Feature customization", 400f, DoFeatureConfigSection, DoFeatureConfigSectionButtons);
        _layout.Abs(Margin);
        DoSectionFrame(null, -1, DoBottomRow);
        _layout.End();

        _layout.End();
    }

    private void DoSectionFrame(string label, float height, Action content, Action headerButtons = null)
    {
        if (label != null)
        {
            float marginX = 6f;
            float marginY = 2f;

            var textSize = Text.CalcSize(label) + new Vector2(2 * marginX, 2 * marginY);

            _layout.BeginAbs(textSize.y, new LayoutParams { Horizontal = true });

            var labelRect = _layout.Abs(textSize.x);
            GUI.color = _colorSectionHeader;
            GUI.DrawTexture(labelRect, BaseContent.WhiteTex);
            GUI.DrawTexture(_layout.Abs(textSize.y), TexHeaderSlant);
            GUI.color = Color.white;
            Widgets.Label(labelRect.Moved(marginX, marginY), label);

            if (headerButtons != null)
            {
                _layout.BeginRel(-1, new LayoutParams{ Horizontal = true, Reversed = true });
                headerButtons();
                _layout.End();
            }

            _layout.End();
        }

        _layout.BeginAbs(height);

        Widgets.DrawBoxSolid(_layout, _colorSectionBackground);
        content();

        _layout.End();
    }

    private void DoGeneralSection()
    {

    }

    private void DoGeneralSectionButtons()
    {

    }

    private void DoRockTypesSection()
    {

    }

    private void DoRockTypesSectionButtons()
    {
        if (DoIconButton(IconAddElement, new Color(0.1f, 0.55f, 0f)))
        {

        }
    }

    private void DoFeaturesSection()
    {

    }

    private void DoFeaturesSectionButtons()
    {
        if (DoIconButton(IconAddElement, new Color(0.1f, 0.55f, 0f)))
        {

        }
    }

    private void DoFeatureConfigSection()
    {

    }

    private void DoFeatureConfigSectionButtons()
    {

    }

    private void DoBottomRow()
    {

    }

    private bool DoIconButton(Texture2D icon, Color color, string tooltip = null)
    {
        var outerRect = (Rect) _layout;
        var btnRect = _layout.Abs(outerRect.height);

        if (tooltip != null)
            TooltipHandler.TipRegionByKey(btnRect, tooltip);

        MouseoverSounds.DoRegion(btnRect);
        Widgets.DrawBoxSolid(btnRect, _colorSectionHeader);

        return Widgets.ButtonImage(btnRect, icon, color);
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
            GUI.DrawTextureWithTexCoords(inRect, Texture, TexCoords);
        }

        protected override void DrawGenerating(Rect inRect)
        {
            DrawPreloader(IconLoading, inRect.center);
        }

        protected override void DrawOutline(Rect rect) {}
    }
}

#endif

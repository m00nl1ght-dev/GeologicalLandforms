#if RW_1_6_OR_GREATER

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using LunarFramework.GUI;
using LunarFramework.Patching;
using LunarFramework.Utility;
using MapPreview;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TileEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public class TileEditorWindow : Window
{
    public static readonly Texture2D IconLoading = ContentFinder<Texture2D>.Get("LoadingIndicatorStaticGL");
    public static readonly Texture2D IconEditTile = ContentFinder<Texture2D>.Get("EditTileIconGL");
    public static readonly Texture2D IconAddElement = ContentFinder<Texture2D>.Get("AddElementIconGL");
    public static readonly Texture2D IconEditElement = ContentFinder<Texture2D>.Get("EditElementIconGL");
    public static readonly Texture2D IconRemoveElement = ContentFinder<Texture2D>.Get("RemoveElementIconGL");
    public static readonly Texture2D IconReset = ContentFinder<Texture2D>.Get("ResetIconGL");
    public static readonly Texture2D IconReroll = ContentFinder<Texture2D>.Get("RerollIconGL");
    public static readonly Texture2D TexHeaderSlant = ContentFinder<Texture2D>.Get("HeaderSlantGL");

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(TileEditorWindow));

    public override Vector2 InitialSize => new(1028, 800);

    public readonly World World;
    public readonly PlanetTile Tile;
    public readonly SurfaceTile TileObj;

    private readonly LayoutRect Layout = new(GeologicalLandformsMod.LunarAPI);

    private readonly MapPreviewWidget PreviewWidget = new(MapSizeUtility.MaxMapSize);

    private static readonly Color ColorSectionHeader = new(0.15f, 0.15f, 0.15f);
    private static readonly Color ColorSectionBackground = new(0.2f, 0.2f, 0.2f);

    private const int PreviewDebounceTime = 200;

    private string _seasonStringCache;

    public static bool CanEditTile(PlanetTile tile)
    {
        if (!tile.Valid || !tile.Layer.IsRootSurface || !MapPreviewAPI.IsReadyForPreviewGen) return false;
        if (Find.WorldObjects.MapParentAt(tile) is { HasMap: true }) return false;
        return true;
    }

    public TileEditorWindow(World world, PlanetTile tile)
    {
        Tile = tile;
        World = world;
        TileObj = world.grid.Surface[tile];
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
        const float previewSize = 360f;

        using (Layout.Root(rect))
        {
            using (Layout.Row())
            {
                using (Layout.Col(previewSize))
                {
                    DoSectionFrame("Preview", previewSize, () => PreviewWidget.Draw(Layout), DoPreviewSectionButtons, 0f);
                    Layout.Abs(Margin);
                    DoSectionFrame("General", 187f, DoGeneralSection, DoGeneralSectionButtons);
                    Layout.Abs(Margin);
                    DoSectionFrame("Stone types", -1, DoRockTypesSection, DoRockTypesSectionButtons);
                }

                Layout.Abs(Margin);

                using (Layout.Col())
                {
                    DoSectionFrame("Features", 250f, DoFeaturesSection, DoFeaturesSectionButtons);
                    Layout.Abs(Margin);
                    DoSectionFrame("Feature customization", 380f, DoFeatureConfigSection, DoFeatureConfigSectionButtons);
                    Layout.Abs(Margin);
                    DoSectionFrame(null, -1, DoBottomRow);
                }
            }
        }
    }

    private void DoSectionFrame(string label, float height, Action content, Action headerButtons = null, float margin = -1)
    {
        if (label != null)
        {
            float marginX = 6f;
            float marginY = 2f;

            var textSize = new Vector2(Text.CalcSize(label).x + 2 * marginX, 22f + 2 * marginY);

            using (Layout.Row(textSize.y))
            {
                var labelRect = Layout.Abs(textSize.x);
                GUI.color = ColorSectionHeader;
                GUI.DrawTexture(labelRect, BaseContent.WhiteTex);
                GUI.DrawTexture(Layout.Abs(textSize.y), TexHeaderSlant);
                GUI.color = Color.white;
                Widgets.Label(labelRect.Moved(marginX, marginY), label);

                if (headerButtons != null)
                {
                    using (Layout.RowRev())
                    {
                        headerButtons();
                    }
                }
            }
        }

        using (Layout.Col(height))
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionBackground);

            using (Layout.Col(-1, new() { Margin = new(margin < 0 ? Margin / 2.0001f : margin) }))
            {
                content();
            }
        }
    }

    private void DoPreviewSectionButtons()
    {
        var rerolled = SeedRerollData.IsMapSeedRerolled(World, Tile, out var seed);

        if (DoIconButton(IconReroll, Color.white, 2f))
        {
            unchecked { seed += 1; }
            SeedRerollData.GetFor(World).Commit(Tile, seed, false);
            GeneratePreviewDebounced();
        }

        if (rerolled && DoIconButton(IconReset, Color.white, 3f))
        {
            SeedRerollData.GetFor(World).Reset(Tile, false);
            GeneratePreviewDebounced();
        }
    }

    private void DoGeneralSection()
    {
        using (Layout.Row(24f)) // Biome
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Biome");
            LunarGUI.Label(Layout.Abs(220f).Moved(5f, 2f), TileObj.biome.LabelCap);

            using (Layout.RowRev())
            {
                if (DoIconButton(IconEditElement, Color.white, 2f))
                {

                }
            }
        }

        Layout.Abs(5f);

        using (Layout.Row(24f)) // Hilliness
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Terrain");
            LunarGUI.Label(Layout.Abs(220f).Moved(5f, 2f), TileObj.hilliness.GetLabelCap());

            using (Layout.RowRev())
            {
                if (DoIconButton(IconEditElement, Color.white, 2f))
                {
                    var options = typeof(Hilliness).GetEnumValues().Cast<Hilliness>()
                        .Where(h => h != Hilliness.Undefined && h != Hilliness.Impassable)
                        .Select(e => new FloatMenuOption(e.GetLabelCap(), () => SetHilliness(e)))
                        .ToList();

                    Find.WindowStack.Add(new FloatMenu(options));

                    void SetHilliness(Hilliness value)
                    {
                        TileObj.hilliness = value;
                        GeneratePreview();
                    }
                }
            }
        }

        if (ModsConfig.BiotechActive)
        {
            Layout.Abs(5f);

            using (Layout.Row(24f)) // Pollution
            {
                Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

                LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Pollution");
                LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), TileObj.pollution.ToStringPercent());
                LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref TileObj.pollution, 0f, 1f);
            }
        }

        Layout.Abs(5f);

        using (Layout.Row(24f)) // Rainfall
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Rainfall");
            LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), $"{TileObj.rainfall:F0}mm");
            LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref TileObj.rainfall, 0f, 5000f);
        }

        Layout.Abs(5f);

        using (Layout.Row(24f)) // Temperature
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            Layout.PushChanged();

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Temperature");
            LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), TileObj.temperature.ToStringTemperature());
            LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref TileObj.temperature, -70f, 70f);

            if (Layout.PopChanged())
            {
                _seasonStringCache = null;
            }
        }

        Layout.Abs(5f);

        using (Layout.Row(24f)) // Seasons
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            if (_seasonStringCache == null)
            {
                var tempMin = GenTemperature.MinTemperatureAtTile(Tile).ToStringTemperature();
                var tempMax = GenTemperature.MaxTemperatureAtTile(Tile).ToStringTemperature();
                var growing = GenTemperature.TwelfthsInAverageTemperatureRange(Tile, 6f, 42f);

                _seasonStringCache = $"{tempMin} to {tempMax} ({growing.Count * 5}/{60})";
            }

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Seasons");
            LunarGUI.Label(Layout.Abs(270f).Moved(5f, 2f), _seasonStringCache);
        }
    }

    private void DoGeneralSectionButtons()
    {
        if (DoIconButton(IconReset, Color.white, 3f))
        {

        }
    }

    private void DoRockTypesSection()
    {

    }

    private void DoRockTypesSectionButtons()
    {
        if (DoIconButton(IconAddElement, Color.white))
        {

        }

        if (DoIconButton(IconReset, Color.white, 3f))
        {

        }
    }

    private void DoFeaturesSection()
    {

    }

    private void DoFeaturesSectionButtons()
    {
        if (DoIconButton(IconAddElement, Color.white))
        {

        }

        if (DoIconButton(IconReset, Color.white, 3f))
        {

        }
    }

    private void DoFeatureConfigSection()
    {

    }

    private void DoFeatureConfigSectionButtons()
    {
        if (DoIconButton(IconReset, Color.white, 3f))
        {

        }
    }

    private void DoBottomRow()
    {

    }

    private bool DoIconButton(Texture2D icon, Color color, float shrink = 0f, string tooltip = null)
    {
        var outerRect = (Rect) Layout;
        var btnRect = Layout.Abs(outerRect.height);

        if (tooltip != null)
            TooltipHandler.TipRegionByKey(btnRect, tooltip);

        Widgets.DrawBoxSolid(btnRect, ColorSectionHeader);

        GUI.color = Mouse.IsOver(btnRect) ? GenUI.MouseoverColor : color;
        GUI.DrawTexture(btnRect.ContractedBy(shrink), icon);
        GUI.color = Color.white;

        return Widgets.ButtonInvisible(btnRect);
    }

    private static readonly Stopwatch _debouncer = new();

    private void GeneratePreviewDebounced()
    {
        var running = _debouncer.IsRunning;

        _debouncer.Restart();

        if (!running)
        {
            GeologicalLandformsMod.LunarAPI.LifecycleHooks.DoEnumerator(Debounce());
        }
    }

    private IEnumerator Debounce()
    {
        while (_debouncer.IsRunning && _debouncer.ElapsedMilliseconds < PreviewDebounceTime)
            yield return null;

        _debouncer.Reset();

        GeneratePreview();
    }

    private void GeneratePreview()
    {
        MapPreviewGenerator.Instance.ClearQueue();

        int seed = SeedRerollData.GetMapSeed(World, Tile);
        var mapParent = World.worldObjects.MapParentAt(Tile);
        var mapSize = MapSizeUtility.DetermineMapSize(World, mapParent);

        var request = new MapPreviewRequest(seed, Tile, mapSize)
        {
            TextureSize = new IntVec2(PreviewWidget.Texture.width, PreviewWidget.Texture.height),
            GeneratorDef = mapParent?.MapGeneratorDef ?? MapGeneratorDefOf.Base_Player,
            UseTrueTerrainColors = true,
            UseMinimalMapComponents = true,
            ExistingBuffer = PreviewWidget.Buffer
        };

        MapPreviewGenerator.Instance.QueuePreviewRequest(request);
        PreviewWidget.Await(request);
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

        PreviewWidget.Dispose();

        MapPreviewGenerator.Instance.ClearQueue();

        MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);

        Find.WorldSelector.SelectedTile = Tile;
    }

    [StaticConstructorOnStartup]
    private class MapPreviewWidget : MapPreview.MapPreviewWidget
    {
        public MapPreviewWidget(IntVec2 maxMapSize) : base(maxMapSize) {}

        protected override void DrawGenerated(Rect inRect)
        {
            GUI.color = new Color(1f, 1f, 1f, SpawnInterpolator.value);
            GUI.DrawTextureWithTexCoords(inRect, Texture, TexCoords);
            GUI.color = Color.white;
        }

        protected override void DrawGenerating(Rect rect)
        {
            var position = new Rect(
                rect.center.x - IconLoading.width / 2f,
                rect.center.y - IconLoading.height / 2f,
                IconLoading.width,
                IconLoading.height
            );

            float a = 1f - (1f + Mathf.Sin(Time.time * 3f)) * 0.4f;

            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(position, IconLoading);
            GUI.color = Color.white;
        }

        protected override void DrawOutline(Rect rect) {}
    }
}

#endif

#if RW_1_6_OR_GREATER

using System;
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

    public override Vector2 InitialSize => new(800, 800);

    public readonly World World;
    public readonly PlanetTile Tile;
    public readonly SurfaceTile TileObj;

    private readonly LayoutRect Layout = new(GeologicalLandformsMod.LunarAPI);

    private readonly LayoutRectScrollable LayoutRockTypes = new(GeologicalLandformsMod.LunarAPI);

    private readonly MapPreviewWidget PreviewWidget = new(MapSizeUtility.MaxMapSize);

    public static readonly Color ColorSectionHeader = new(0.15f, 0.15f, 0.15f);
    public static readonly Color ColorSectionBackground = new(0.2f, 0.2f, 0.2f);

    private readonly TileEditorData _dataOriginal = new();
    private readonly TileEditorData _dataBefore = new();
    private readonly TileEditorData _data = new();

    private MapPreviewRequest _currentPreviewRequest;
    private bool _previewNeedsRefresh;

    private string _seasonStringCache;

    public static bool CanEditTile(PlanetTile tile)
    {
        if (!tile.Valid || !tile.Layer.IsRootSurface || !MapPreviewAPI.IsReadyForPreviewGen) return false;
        if (Find.WorldObjects.MapParentAt(tile) is { HasMap: true }) return false;
        if (!Find.WorldGrid[tile].PrimaryBiome.canBuildBase) return false;
        if (Find.World.LandformData() == null) return false;
        return true;
    }

    public TileEditorWindow(World world, PlanetTile tile)
    {
        Tile = tile;
        World = world;

        TileObj = world.grid.Surface[tile];

        _dataOriginal.ReadOriginal(tile);
        _dataBefore.Read(tile);
        _data.Read(tile);

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

            using (Layout.Col(-1, new() { Margin = new(margin < 0 ? Margin / 2.0001f : margin), Spacing = 5f }))
            {
                content();
            }
        }
    }

    private void DoPreviewSectionButtons()
    {
        var rerolled = SeedRerollData.IsMapSeedRerolled(World, Tile, out var seed);

        if (DoIconButton(Layout, IconReroll, Color.white, 2f))
        {
            unchecked { seed += 1; }
            SeedRerollData.GetFor(World).Commit(Tile, seed, false);
            PreviewNeedsRefresh();
        }

        if (rerolled && DoIconButton(Layout, IconReset, Color.white, 3f))
        {
            SeedRerollData.GetFor(World).Reset(Tile, false);
            PreviewNeedsRefresh();
        }
    }

    private void DoGeneralSection()
    {
        using (Layout.Row(24f)) // Biome
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Biome");
            LunarGUI.Label(Layout.Abs(220f).Moved(5f, 2f), _data.Biome.LabelCap);

            using (Layout.RowRev())
            {
                if (DoIconButton(Layout, IconEditElement, Color.white, 2f))
                {
                    Find.WindowStack.Add(new DefSelectionWindow<BiomeDef>(SetBiome, BiomeFilter));

                    void SetBiome(BiomeDef value)
                    {
                        _data.Biome = value;
                        PreviewNeedsRefresh();
                    }

                    bool BiomeFilter(BiomeDef def)
                    {
                        return def.generatesNaturally && def.canBuildBase;
                    }
                }
            }
        }

        using (Layout.Row(24f)) // Hilliness
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Terrain");
            LunarGUI.Label(Layout.Abs(220f).Moved(5f, 2f), _data.Hilliness.GetLabelCap());

            using (Layout.RowRev())
            {
                if (DoIconButton(Layout, IconEditElement, Color.white, 2f))
                {
                    var options = typeof(Hilliness).GetEnumValues().Cast<Hilliness>()
                        .Where(h => h != Hilliness.Undefined && h != Hilliness.Impassable)
                        .Select(e => new FloatMenuOption(e.GetLabelCap(), () => SetHilliness(e)))
                        .ToList();

                    Find.WindowStack.Add(new FloatMenu(options));

                    void SetHilliness(Hilliness value)
                    {
                        _data.Hilliness = value;
                        PreviewNeedsRefresh();
                    }
                }
            }
        }

        if (ModsConfig.BiotechActive)
        {
            using (Layout.Row(24f)) // Pollution
            {
                Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

                LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Pollution");
                LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), _data.Pollution.ToStringPercent());
                LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref _data.Pollution, 0f, 1f);
            }
        }

        using (Layout.Row(24f)) // Rainfall
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Rainfall");
            LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), $"{_data.Rainfall:F0}mm");
            LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref _data.Rainfall, 0f, 5000f);
        }

        using (Layout.Row(24f)) // Temperature
        {
            Widgets.DrawBoxSolid(Layout, ColorSectionHeader);

            Layout.PushChanged();

            LunarGUI.Label(Layout.Abs(110f).Moved(5f, 2f), "Temperature");
            LunarGUI.Label(Layout.Abs(70f).Moved(5f, 2f), _data.Temperature.ToStringTemperature());
            LunarGUI.Slider(Layout.Abs(-1).Moved(-3f, 7f), ref _data.Temperature, -70f, 70f);

            if (Layout.PopChanged())
            {
                TileObj.temperature = _data.Temperature;
                _seasonStringCache = null;
            }
        }

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
        /*

        if (DoIconButton(Layout, IconReset, Color.white, 3f))
        {
            _data.ResetGeneral(Tile);
            PreviewNeedsRefresh();
        }

        */
    }

    private void DoRockTypesSection()
    {
        using (LayoutRockTypes.RootScrollable(Layout.Rel(-1), new() { Spacing = 5f }))
        {
            foreach (var rockTypeEntry in _data.RockTypes.ToList())
            {
                using (LayoutRockTypes.Row(24f))
                {
                    Widgets.DrawBoxSolid(LayoutRockTypes, ColorSectionHeader);

                    LunarGUI.Label(LayoutRockTypes.Abs(110f).Moved(5f, 2f), rockTypeEntry.Key.LabelCap);
                    LunarGUI.Label(LayoutRockTypes.Abs(70f).Moved(5f, 2f), $"{rockTypeEntry.Value:F2}");

                    using (LayoutRockTypes.RowRev())
                    {
                        if (_data.RockTypes.Count > 1 && DoIconButton(LayoutRockTypes, IconRemoveElement, Color.red, 2f))
                        {
                            _data.RockTypes.Remove(rockTypeEntry.Key);
                            PreviewNeedsRefresh();
                        }

                        var value = rockTypeEntry.Value;

                        LunarGUI.Slider(LayoutRockTypes.Abs(-1).Moved(-3f, 7f), ref value, -1f, 1f);

                        if (value != rockTypeEntry.Value)
                        {
                            _data.RockTypes[rockTypeEntry.Key] = value;
                            PreviewNeedsRefresh();
                        }
                    }
                }
            }
        }
    }

    private void DoRockTypesSectionButtons()
    {
        if (DoIconButton(Layout, IconAddElement, Color.white))
        {
            Find.WindowStack.Add(new DefSelectionWindow<ThingDef>(AddRockType, RockTypeFilter));

            void AddRockType(ThingDef value)
            {
                _data.RockTypes.Add(value, 0f);
                PreviewNeedsRefresh();
            }

            bool RockTypeFilter(ThingDef def)
            {
                if (!def.IsNonResourceNaturalRock) return false;
                if (_data.RockTypes.ContainsKey(def)) return false;
                return true;
            }
        }

        if (DoIconButton(Layout, IconReset, Color.white, 3f))
        {
            _data.CopyRockTypes(_dataOriginal);
            PreviewNeedsRefresh();
        }
    }

    private void DoFeaturesSection()
    {

    }

    private void DoFeaturesSectionButtons()
    {
        if (DoIconButton(Layout, IconAddElement, Color.white))
        {

        }

        if (DoIconButton(Layout, IconReset, Color.white, 3f))
        {
            _data.CopyFeatures(_dataOriginal);
            PreviewNeedsRefresh();
        }
    }

    private void DoFeatureConfigSection()
    {

    }

    private void DoFeatureConfigSectionButtons()
    {
        if (DoIconButton(Layout, IconReset, Color.white, 3f))
        {

        }
    }

    private void DoBottomRow()
    {

    }

    private bool DoIconButton(LayoutRect layout, Texture2D icon, Color color, float shrink = 0f, string tooltip = null)
    {
        var outerRect = (Rect) layout;
        var btnRect = layout.Abs(outerRect.height);

        if (tooltip != null)
            TooltipHandler.TipRegionByKey(btnRect, tooltip);

        Widgets.DrawBoxSolid(btnRect, ColorSectionHeader);

        GUI.color = Mouse.IsOver(btnRect) ? GenUI.MouseoverColor : color;
        GUI.DrawTexture(btnRect.ContractedBy(shrink), icon);
        GUI.color = Color.white;

        return Widgets.ButtonInvisible(btnRect);
    }

    private void PreviewNeedsRefresh()
    {
        if (_currentPreviewRequest is { Pending: true })
        {
            _previewNeedsRefresh = true;
            return;
        }

        _data.Apply(Tile, _dataOriginal);

        MapPreviewGenerator.Instance.ClearQueue();

        int seed = SeedRerollData.GetMapSeed(World, Tile);
        var mapParent = World.worldObjects.MapParentAt(Tile);
        var mapSize = MapSizeUtility.DetermineMapSize(World, mapParent);

        _currentPreviewRequest = new MapPreviewRequest(seed, Tile, mapSize)
        {
            TextureSize = new IntVec2(PreviewWidget.Texture.width, PreviewWidget.Texture.height),
            GeneratorDef = mapParent?.MapGeneratorDef ?? MapGeneratorDefOf.Base_Player,
            UseTrueTerrainColors = true,
            UseMinimalMapComponents = true,
            ExistingBuffer = PreviewWidget.Buffer
        };

        PreviewWidget.Await(_currentPreviewRequest);

        _currentPreviewRequest.Promise.Done(r =>
        {
            if (_previewNeedsRefresh)
            {
                _previewNeedsRefresh = false;
                PreviewNeedsRefresh();
            }
        });

        MapPreviewGenerator.Instance.QueuePreviewRequest(_currentPreviewRequest);
    }

    public override void PreOpen()
    {
        Find.WorldSelector.ClearSelection();
        CompatUtils.CloseAllMapPreviewWindows();

        base.PreOpen();

        MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);

        MapPreviewGenerator.Init();

        PreviewNeedsRefresh();
    }

    public override void PreClose()
    {
        base.PreClose();

        _previewNeedsRefresh = false;

        PreviewWidget.Dispose();

        MapPreviewGenerator.Instance.ClearQueue();

        MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);

        Find.WorldSelector.SelectedTile = Tile;

        if (_data.Temperature != _dataBefore.Temperature)
        {
            Find.World.tileTemperatures.ClearCaches();
        }

        if (_data.Biome != _dataBefore.Biome)
        {
            Find.World.grid.Surface.SetDirty<WorldDrawLayer_Terrain>();
        }

        if (_data.Hilliness != _dataBefore.Hilliness)
        {
            Find.World.grid.Surface.SetDirty<WorldDrawLayer_Hills>();
        }
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

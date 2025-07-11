using System;
using System.Collections.Generic;
using System.Linq;
using LunarFramework.GUI;
using NodeEditorFramework;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static GeologicalLandforms.Topology;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/Tile Requirements", 1000)]
public class NodeUIWorldTileReq : NodeUIBase
{
    public const string ID = "worldTileReq";
    public override string GetID => ID;

    public override string Title => "World Tile Requirements";
    public override Vector2 DefaultSize => new(400, _lastLayoutMaxY);

    public Topology Topology;

    public float Commonness = 1f;
    public float CaveChance;

    public List<AllowedBiome> AllowedBiomes;
    public List<RiverType> AllowedRiverTypes;

    public List<WorldObjectNearby> WorldObjectsNearby;

    public FloatRange HillinessRequirement;
    public FloatRange RoadRequirement;
    public FloatRange RiverRequirement;
    public FloatRange ElevationRequirement;
    public FloatRange AvgTemperatureRequirement;
    public FloatRange RainfallRequirement;
    public FloatRange SwampinessRequirement;
    public FloatRange BiomeTransitionsRequirement;
    public FloatRange TopologyValueRequirement;
    public FloatRange DepthInCaveSystemRequirement;
    public FloatRange MapSizeRequirement;

    public bool AllowSettlements;
    public bool AllowSites;

    private List<ICondition> _activeConditions = [];

    public NodeUIWorldTileReq()
    {
        foreach (var condition in Conditions) condition.Reset(this, false);
    }

    public float GetCommonnessForTile(IWorldTileInfo worldTile, bool lenient)
    {
        var value = Commonness;

        foreach (var condition in _activeConditions)
        {
            if (condition.IsActive(this))
            {
                value *= condition.GetScore(this, worldTile, lenient);
                if (value <= 0f) return 0f;
            }
        }

        #if RW_1_6_OR_GREATER

        if (worldTile.Landmark != null && !Landform.IsLayer)
        {
            return 0f;
        }

        #endif

        return CheckWorldObject(worldTile) ? value : 0f;
    }

    public bool CheckWorldObject(IWorldTileInfo worldTile)
    {
        var worldObject = worldTile.WorldObject;
        if (worldObject == null || worldObject.Faction is { IsPlayer: true }) return true;
        if (!AllowSettlements && worldObject is Settlement) return false;
        if (!AllowSites && worldObject is not Settlement) return false;
        return true;
    }

    private const string LabelKeyPrefix = "GeologicalLandforms.Settings.Landform";

    private float _lastLayoutMaxY = 400f;

    protected override void DoWindowContents(LayoutRect layout)
    {
        LunarGUI.LabelDouble(layout, $"{LabelKeyPrefix}.Commonness".Translate(), Commonness.ToString("F2"));
        LunarGUI.Slider(layout, ref Commonness, 0f, 1f);

        layout.PushEnabled(!Landform.IsLayer);

        LunarGUI.LabelDouble(layout, $"{LabelKeyPrefix}.CaveChance".Translate(), CaveChance.ToString("F2"));
        LunarGUI.Slider(layout, ref CaveChance, 0f, 1f);

        layout.PopEnabled();

        for (var i = 0; i < _activeConditions.Count; i++)
        {
            var rect1 = layout.Abs(10f);

            var condition = _activeConditions[i];
            condition.EditorGUI(this, layout);

            var rect2 = layout.Abs(10f);

            var rect3 = new Rect(rect1.xMin, rect1.yMax, rect1.width, rect2.yMin - rect1.yMax);

            if (Mouse.IsOver(rect3))
            {
                var rect4 = new Rect(rect1.xMax - 20f, rect1.yMax, 20f, 20f);

                if (Widgets.ButtonImage(rect4, UserInterfaceUtils.IconDismiss))
                {
                    condition.Reset(this, true);
                    _activeConditions.RemoveAt(i);
                    i--;
                }
            }
        }

        layout.Abs(15f);

        if (LunarGUI.Button(layout, $"{LabelKeyPrefix}.AddRequirement".Translate()))
        {
            var options = Conditions
                .Except(_activeConditions)
                .Select(c => new FloatMenuOption($"{LabelKeyPrefix}.{c.LabelKey}".Translate(), () => Add(c)))
                .ToList();

            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        layout.Abs(20f);

        DoCheckbox(layout.Abs(24f), ref AllowSettlements, $"{LabelKeyPrefix}.AllowSettlements".Translate());

        layout.Abs(10f);

        DoCheckbox(layout.Abs(24f), ref AllowSites, $"{LabelKeyPrefix}.AllowSites".Translate());

        _lastLayoutMaxY = layout.OccupiedSpace + 50f;
    }

    private void Add(ICondition condition)
    {
        _activeConditions.Add(condition);
        _activeConditions = Conditions.Where(c => _activeConditions.Contains(c)).ToList();
    }

    public override void DrawNode()
    {
        if (Landform.Id != null) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.WorldTileReq;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.WorldTileReq = this;

        _activeConditions.Clear();
        _activeConditions.AddRange(Conditions.Where(c => c.IsActive(this)));
    }

    protected override void OnDelete()
    {
        if (Landform.WorldTileReq == this) Landform.WorldTileReq = null;
    }

    [Serializable]
    public struct WorldObjectNearby
    {
        public string WorldObjectDef;
        public FloatRange DistanceRange;

        public WorldObjectNearby(WorldObjectDef def)
        {
            WorldObjectDef = def.defName;
            DistanceRange = new FloatRange(0f, 100f);
        }
    }

    [Serializable]
    public struct AllowedBiome
    {
        public string BiomeDef;
        public float Commonness;

        public AllowedBiome(BiomeDef def)
        {
            BiomeDef = def.defName;
            Commonness = 1f;
        }
    }

    private static readonly IReadOnlyList<ICondition> Conditions = typeof(NodeUIWorldTileReq).Assembly.GetTypes()
        .Where(t => typeof(ICondition).IsAssignableFrom(t) && !t.IsAbstract)
        .Select(Activator.CreateInstance)
        .Cast<ICondition>()
        .ToList();

    private interface ICondition
    {
        public string LabelKey { get; }

        public bool IsActive(NodeUIWorldTileReq node);

        public void Reset(NodeUIWorldTileReq node, bool clear);

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient);

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout);
    }

    private abstract class FloatRangeCondition : ICondition
    {
        public abstract string LabelKey { get; }

        public virtual FloatRange AllowedRange => new(0f, 1f);
        public virtual FloatRange DefaultRange => AllowedRange;

        protected abstract FloatRange Get(NodeUIWorldTileReq node);
        protected abstract void Set(NodeUIWorldTileReq node, FloatRange range);
        protected abstract float GetScore(IWorldTileInfo tile, FloatRange range);

        public bool IsActive(NodeUIWorldTileReq node) => Get(node) != AllowedRange;
        public void Reset(NodeUIWorldTileReq node, bool clear) => Set(node, clear ? AllowedRange : DefaultRange);
        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) => GetScore(tile, Get(node));

        public virtual void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            var range = Get(node);
            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());
            LunarGUI.RangeSlider(layout, ref range, AllowedRange.min, AllowedRange.max);
            Set(node, range);
        }
    }

    private class TopologyCondition : ICondition
    {
        public string LabelKey => "Topology";

        public bool IsActive(NodeUIWorldTileReq node) => node.Topology != Any;

        public void Reset(NodeUIWorldTileReq node, bool clear) => node.Topology = clear ? Any : Inland;

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) =>
            node.Topology.IsCompatible(tile.Topology, lenient) ? 1f : 0f;

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());

            layout.Abs(3f);

            LunarGUI.Dropdown(layout, node.Topology, e => node.Topology = e, $"{LabelKeyPrefix}.{LabelKey}");
        }
    }

    private class BiomeCondition : ICondition
    {
        public string LabelKey => "AllowedBiomes";

        public bool IsActive(NodeUIWorldTileReq node) => node.AllowedBiomes != null;

        public void Reset(NodeUIWorldTileReq node, bool clear) => node.AllowedBiomes = null;

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient)
        {
            var entry = node.AllowedBiomes.FirstOrDefault(e => e.BiomeDef == tile.Biome.defName);
            if (entry.BiomeDef != null) return entry.Commonness;
            return node.AllowedBiomes.FirstOrDefault(e => e.BiomeDef == null).Commonness;
        }

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            node.AllowedBiomes ??= [];

            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());

            layout.Abs(3f);

            if (LunarGUI.Button(layout, $"{LabelKeyPrefix}.{LabelKey}.Some".Translate(node.AllowedBiomes.Count)))
            {
                LunarGUI.OpenGenericWindow(GeologicalLandformsAPI.LunarAPI, new(500, 500), (_, layoutW) =>
                {
                    for (var i = 0; i < node.AllowedBiomes.Count; i++)
                    {
                        var entry = node.AllowedBiomes[i];

                        layoutW.PushChanged();

                        layoutW.BeginAbs(28f, new LayoutParams { Horizontal = true, Spacing = 10f });

                        var def = entry.BiomeDef == null ? null : DefDatabase<BiomeDef>.GetNamedSilentFail(entry.BiomeDef);
                        var label = def == null ? entry.BiomeDef ?? "Any unspecified biome" : def.LabelCap.ToString();

                        LunarGUI.Label(layoutW.Abs(210f), label);
                        LunarGUI.Label(layoutW.Abs(30f), $"{entry.Commonness:F2}");
                        LunarGUI.Slider(layoutW.Abs(160f).Moved(0f, 4f), ref entry.Commonness, 0f, 1f);

                        var removed = Widgets.ButtonImage(layoutW.Abs(21f).TopPart(0.75f).Moved(0f, -1f), UserInterfaceUtils.IconDelete);

                        layoutW.End();

                        if (removed)
                        {
                            node.AllowedBiomes.RemoveAt(i);
                            i--;
                        }
                        else if (layoutW.PopChanged())
                        {
                            node.AllowedBiomes[i] = entry;
                        }
                    }

                    if (LunarGUI.Button(layoutW, "Add entry"))
                    {
                        var options = DefDatabase<BiomeDef>.AllDefsListForReading
                            .Where(d => d.Properties().AllowsLandform(node.Landform))
                            .Where(d => !node.AllowedBiomes.Any(e => e.BiomeDef == d.defName))
                            .Select(e => new FloatMenuOption(e.LabelCap,
                                () => node.AllowedBiomes.Add(new AllowedBiome(e))))
                            .ToList();

                        options.Add(new FloatMenuOption("Any unspecified biome",
                            () => node.AllowedBiomes.Add(new AllowedBiome())));

                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }
                });
            }
        }
    }

    private class WorldObjectNearbyCondition : ICondition
    {
        public string LabelKey => "WorldObjectNearby";

        public bool IsActive(NodeUIWorldTileReq node) => node.WorldObjectsNearby != null;

        public void Reset(NodeUIWorldTileReq node, bool clear) => node.WorldObjectsNearby = null;

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient)
        {
            var bestScore = 0f;

            var posInWorld = tile.PosInWorld;

            foreach (var entry in node.WorldObjectsNearby)
            {
                var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail(entry.WorldObjectDef);

                if (def != null)
                {
                    var distance = WorldTileUtils.DistanceToNearestWorldObject(Find.World, posInWorld, def);
                    var score = entry.DistanceRange.SmoothDist(new(0f, 100f), distance, 1f);
                    if (score > bestScore) bestScore = score;
                }
            }

            return bestScore;
        }

        private string LabelFor(WorldObjectDef def)
        {
            return string.IsNullOrEmpty(def.label) ? def.defName : def.label.CapitalizeFirst();
        }

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            node.WorldObjectsNearby ??= [];

            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());

            layout.Abs(3f);

            if (LunarGUI.Button(layout, $"{LabelKeyPrefix}.{LabelKey}.Some".Translate(node.WorldObjectsNearby.Count)))
            {
                LunarGUI.OpenGenericWindow(GeologicalLandformsAPI.LunarAPI, new(500, 500), (_, layoutW) =>
                {
                    for (var i = 0; i < node.WorldObjectsNearby.Count; i++)
                    {
                        var entry = node.WorldObjectsNearby[i];

                        layoutW.PushChanged();

                        layoutW.BeginAbs(28f, new LayoutParams { Horizontal = true, Spacing = 10f });

                        var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail(entry.WorldObjectDef);

                        LunarGUI.Label(layoutW.Abs(210f).Moved(0f, 8f), def == null ? entry.WorldObjectDef : LabelFor(def));
                        LunarGUI.RangeSlider(layoutW.Abs(190f), ref entry.DistanceRange, 0f, 100f);

                        var removed = Widgets.ButtonImage(layoutW.Abs(21f).TopPart(0.75f).Moved(0f, 9f), UserInterfaceUtils.IconDelete);

                        layoutW.End();

                        if (removed)
                        {
                            node.WorldObjectsNearby.RemoveAt(i);
                            i--;
                        }
                        else if (layoutW.PopChanged())
                        {
                            node.WorldObjectsNearby[i] = entry;
                        }
                    }

                    layoutW.Abs(5f);

                    if (LunarGUI.Button(layoutW, "Add entry"))
                    {
                        var options = DefDatabase<WorldObjectDef>.AllDefsListForReading
                            .Where(d => !d.isTempIncidentMapOwner && !d.allowCaravanIncidentsWhichGenerateMap)
                            .Where(d => !node.WorldObjectsNearby.Any(e => e.WorldObjectDef == d.defName))
                            .Select(e => new FloatMenuOption(LabelFor(e), () => node.WorldObjectsNearby.Add(new WorldObjectNearby(e))))
                            .ToList();

                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }
                });
            }
        }
    }

    private class RiverTypeCondition : ICondition
    {
        public string LabelKey => "AllowedRiverTypes";

        public bool IsActive(NodeUIWorldTileReq node) => node.AllowedRiverTypes != null;

        public void Reset(NodeUIWorldTileReq node, bool clear) => node.AllowedRiverTypes = null;

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) =>
            node.AllowedRiverTypes.Contains(tile.RiverType) ? 1f : 0f;

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            node.AllowedRiverTypes ??= [];

            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());

            layout.Abs(3f);

            if (LunarGUI.Button(layout, $"{LabelKeyPrefix}.{LabelKey}.Some".Translate(node.AllowedRiverTypes.Count)))
            {
                LunarGUI.OpenGenericWindow(GeologicalLandformsAPI.LunarAPI, new(500, 500), (_, layoutW) =>
                {
                    foreach (var riverType in typeof(RiverType).GetEnumValues().Cast<RiverType>())
                    {
                        LunarGUI.ToggleTableRow(layoutW, riverType, false, riverType.ToString(), node.AllowedRiverTypes);
                    }
                });
            }
        }
    }

    private class HillinessCondition : FloatRangeCondition
    {
        public override string LabelKey => "HillinessRequirement";

        public override FloatRange AllowedRange => new(1f, 6f);
        public override FloatRange DefaultRange => new(1f, 5f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.HillinessRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.HillinessRequirement = range;

        protected override float GetScore(IWorldTileInfo tile, FloatRange range)
        {
            if (tile.Hilliness == Hilliness.Impassable) return range.max > 5f ? 1f : 0f;
            return range.Includes((float) tile.Hilliness) ? 1f : 0f;
        }

        private string LabelFor(float val)
        {
            return (val > 5f ? Hilliness.Impassable : (Hilliness) (int) Math.Min(val, 4f)).GetLabelCap();
        }

        public override void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            var range = Get(node);
            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());
            LunarGUI.RangeSlider(layout, ref range, AllowedRange.min, AllowedRange.max, LabelFor);
            Set(node, range);
        }
    }

    private class ElevationCondition : FloatRangeCondition
    {
        public override string LabelKey => "ElevationRequirement";

        public override FloatRange AllowedRange => new(-1000f, 5000f);
        public override FloatRange DefaultRange => new(0f, 5000f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.ElevationRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.ElevationRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.SmoothDist(DefaultRange, tile.Elevation);
    }

    private class TopologyValueCondition : FloatRangeCondition
    {
        public override string LabelKey => "TopologyValueRequirement";

        public override FloatRange AllowedRange => new(-1f, 1f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.TopologyValueRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.TopologyValueRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.TopologyValue) ? 1f : 0f;
    }

    private class RiverOrRoadCondition : ICondition
    {
        public string LabelKey => "RiverOrRoadRequirement";

        public FloatRange AllowedRange => new(0f, 1f);

        public bool IsActive(NodeUIWorldTileReq node)
        {
            return node.RiverRequirement != AllowedRange || node.RoadRequirement != AllowedRange;
        }

        public void Reset(NodeUIWorldTileReq node, bool clear)
        {
            node.RiverRequirement = AllowedRange;
            node.RoadRequirement = AllowedRange;
        }

        public float GetScore(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient)
        {
            var river = tile.MainRiver.WidthOnWorld();
            var road = tile.MainRoad.WidthOnWorld();

            if (river > node.RiverRequirement.max && node.RiverRequirement.max < 1f) return 0f;
            if (road > node.RoadRequirement.max && node.RoadRequirement.max < 1f) return 0f;
            if (node.RiverRequirement.min <= 0f && node.RoadRequirement.min <= 0f) return 1f;
            if (node.RiverRequirement.min > 0f && river >= node.RiverRequirement.min) return 1f;
            if (node.RoadRequirement.min > 0f && road >= node.RoadRequirement.min) return 1f;

            return 0f;
        }

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.RiverRequirement".Translate());
            LunarGUI.RangeSlider(layout, ref node.RiverRequirement, AllowedRange.min, AllowedRange.max);

            layout.Abs(20f);

            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.RoadRequirement".Translate());
            LunarGUI.RangeSlider(layout, ref node.RoadRequirement, AllowedRange.min, AllowedRange.max);
        }
    }

    private class AvgTemperatureCondition : FloatRangeCondition
    {
        public override string LabelKey => "AvgTemperatureRequirement";

        public override FloatRange AllowedRange => new(-100f, 100f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.AvgTemperatureRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.AvgTemperatureRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.SmoothDist(DefaultRange, tile.Temperature);
    }

    private class RainfallCondition : FloatRangeCondition
    {
        public override string LabelKey => "RainfallRequirement";

        public override FloatRange AllowedRange => new(0f, 5000f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.RainfallRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.RainfallRequirement = range;

        protected override float GetScore(IWorldTileInfo tile, FloatRange range)
        {
            if (tile.Rainfall > AllowedRange.max && range.max == AllowedRange.max) return 1f;
            return range.SmoothDist(DefaultRange, tile.Rainfall);
        }
    }

    private class SwampinessCondition : FloatRangeCondition
    {
        public override string LabelKey => "SwampinessRequirement";

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.SwampinessRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.SwampinessRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.SmoothDist(DefaultRange, tile.Swampiness);
    }

    private class BiomeTransitionsCondition : FloatRangeCondition
    {
        public override string LabelKey => "BiomeTransitionsRequirement";

        public override FloatRange AllowedRange => new(0f, 6f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.BiomeTransitionsRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.BiomeTransitionsRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.BorderingBiomesCount()) ? 1f : 0f;
    }

    private class DepthInCaveSystemCondition : FloatRangeCondition
    {
        public override string LabelKey => "DepthInCaveSystemRequirement";

        public override FloatRange AllowedRange => new(0f, 10f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.DepthInCaveSystemRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.DepthInCaveSystemRequirement = range;
        protected override float GetScore(IWorldTileInfo tile, FloatRange range) => range.SmoothDist(new(1f, 10f), tile.DepthInCaveSystem);
    }
}

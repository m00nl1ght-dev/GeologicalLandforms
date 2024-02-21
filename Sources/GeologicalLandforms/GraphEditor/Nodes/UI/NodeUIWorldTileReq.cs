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

    public List<string> AllowedBiomes;
    public List<RiverType> AllowedRiverTypes;

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

    public bool CheckRequirements(IWorldTileInfo worldTile, bool lenient)
    {
        return _activeConditions.All(c => !c.IsActive(this) || c.Check(this, worldTile, lenient));
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

        LunarGUI.Checkbox(layout, ref AllowSettlements, $"{LabelKeyPrefix}.AllowSettlements".Translate());

        layout.Abs(10f);

        LunarGUI.Checkbox(layout, ref AllowSites, $"{LabelKeyPrefix}.AllowSites".Translate());

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

        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient);

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout);
    }

    private abstract class FloatRangeCondition : ICondition
    {
        public abstract string LabelKey { get; }

        public virtual FloatRange AllowedRange => new(0f, 1f);
        public virtual FloatRange DefaultRange => AllowedRange;

        protected abstract FloatRange Get(NodeUIWorldTileReq node);
        protected abstract void Set(NodeUIWorldTileReq node, FloatRange range);
        protected abstract bool Check(IWorldTileInfo tile, FloatRange range);

        public bool IsActive(NodeUIWorldTileReq node) => Get(node) != AllowedRange;
        public void Reset(NodeUIWorldTileReq node, bool clear) => Set(node, clear ? AllowedRange : DefaultRange);
        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) => Check(tile, Get(node));

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

        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) =>
            node.Topology.IsCompatible(tile.Topology, lenient);

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

        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) =>
            node.AllowedBiomes.Contains(tile.Biome.defName);

        public void EditorGUI(NodeUIWorldTileReq node, LayoutRect layout)
        {
            node.AllowedBiomes ??= [];

            LunarGUI.LabelCentered(layout, $"{LabelKeyPrefix}.{LabelKey}".Translate());

            layout.Abs(3f);

            if (LunarGUI.Button(layout, $"{LabelKeyPrefix}.{LabelKey}.Some".Translate(node.AllowedBiomes.Count)))
            {
                LunarGUI.OpenGenericWindow(GeologicalLandformsAPI.LunarAPI, new(500, 500), (_, layoutW) =>
                {
                    foreach (var biome in DefDatabase<BiomeDef>.AllDefsListForReading)
                    {
                        var props = biome.Properties();
                        var label = UserInterfaceUtils.LabelForBiome(biome, false);
                        var allowed = props.AllowsLandform(node.Landform);

                        if (!allowed && biome.modContentPack is { IsOfficialMod: true }) continue;

                        LunarGUI.ToggleTableRow(layoutW, biome.defName, false, label, allowed ? node.AllowedBiomes : null);
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

        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient) =>
            node.AllowedRiverTypes.Contains(tile.River);

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

        protected override bool Check(IWorldTileInfo tile, FloatRange range) =>
            tile.Hilliness == Hilliness.Impassable ? range.max > 5f : range.Includes((float) tile.Hilliness);

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
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.Elevation);
    }

    private class TopologyValueCondition : FloatRangeCondition
    {
        public override string LabelKey => "TopologyValueRequirement";

        public override FloatRange AllowedRange => new(-1f, 1f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.TopologyValueRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.TopologyValueRequirement = range;
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.TopologyValue);
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

        public bool Check(NodeUIWorldTileReq node, IWorldTileInfo tile, bool lenient)
        {
            var river = tile.MainRiver.WidthOnWorld();
            var road = tile.MainRoad.WidthOnWorld();

            if (river > node.RiverRequirement.max && node.RiverRequirement.max < 1f) return false;
            if (road > node.RoadRequirement.max && node.RoadRequirement.max < 1f) return false;
            if (node.RiverRequirement.min > 0f) return river >= node.RiverRequirement.min;
            if (node.RoadRequirement.min > 0f) return road >= node.RoadRequirement.min;

            return true;
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
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.Temperature);
    }

    private class RainfallCondition : FloatRangeCondition
    {
        public override string LabelKey => "RainfallRequirement";

        public override FloatRange AllowedRange => new(0f, 5000f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.RainfallRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.RainfallRequirement = range;

        protected override bool Check(IWorldTileInfo tile, FloatRange range)
        {
            return range.Includes(tile.Rainfall) || (tile.Rainfall > AllowedRange.max && range.max == AllowedRange.max);
        }
    }

    private class SwampinessCondition : FloatRangeCondition
    {
        public override string LabelKey => "SwampinessRequirement";

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.SwampinessRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.SwampinessRequirement = range;
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.Swampiness);
    }

    private class BiomeTransitionsCondition : FloatRangeCondition
    {
        public override string LabelKey => "BiomeTransitionsRequirement";

        public override FloatRange AllowedRange => new(0f, 6f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.BiomeTransitionsRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.BiomeTransitionsRequirement = range;
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.BorderingBiomesCount());
    }

    private class DepthInCaveSystemCondition : FloatRangeCondition
    {
        public override string LabelKey => "DepthInCaveSystemRequirement";

        public override FloatRange AllowedRange => new(0f, 10f);

        protected override FloatRange Get(NodeUIWorldTileReq node) => node.DepthInCaveSystemRequirement;
        protected override void Set(NodeUIWorldTileReq node, FloatRange range) => node.DepthInCaveSystemRequirement = range;
        protected override bool Check(IWorldTileInfo tile, FloatRange range) => range.Includes(tile.DepthInCaveSystem);
    }
}

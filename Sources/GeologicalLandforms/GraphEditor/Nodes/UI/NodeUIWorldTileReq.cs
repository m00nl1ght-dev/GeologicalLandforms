using System;
using System.Collections.Generic;
using LunarFramework.GUI;
using NodeEditorFramework;
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
    public override Vector2 DefaultSize => new(400, 860);

    public Topology Topology = Inland;
    public float Commonness = 1f;
    public float CaveChance;

    public static readonly FloatRange DefaultHillinessRequirement = new(1f, 5f);
    public static readonly FloatRange DefaultRoadRequirement = new(0f, 1f);
    public static readonly FloatRange DefaultRiverRequirement = new(0f, 1f);
    public static readonly FloatRange DefaultElevationRequirement = new(0f, 5000f);
    public static readonly FloatRange DefaultAvgTemperatureRequirement = new(-100f, 100f);
    public static readonly FloatRange DefaultRainfallRequirement = new(0f, 5000f);
    public static readonly FloatRange DefaultSwampinessRequirement = new(0f, 1f);
    public static readonly FloatRange DefaultMapSizeRequirement = new(250f, 1000f);
    public static readonly FloatRange DefaultBiomeTransitionsRequirement = new(0f, 6f);
    public static readonly FloatRange DefaultTopologyValueRequirement = new(-1f, 1f);
    public static readonly FloatRange DefaultDepthInCaveSystemRequirement = new(0f, 10f);

    public FloatRange HillinessRequirement = DefaultHillinessRequirement;
    public FloatRange RoadRequirement = DefaultRoadRequirement;
    public FloatRange RiverRequirement = DefaultRiverRequirement;
    public FloatRange ElevationRequirement = DefaultElevationRequirement;
    public FloatRange AvgTemperatureRequirement = DefaultAvgTemperatureRequirement;
    public FloatRange RainfallRequirement = DefaultRainfallRequirement;
    public FloatRange SwampinessRequirement = DefaultSwampinessRequirement;
    public FloatRange MapSizeRequirement = DefaultMapSizeRequirement;
    public FloatRange BiomeTransitionsRequirement = DefaultBiomeTransitionsRequirement;
    public FloatRange TopologyValueRequirement = DefaultTopologyValueRequirement;
    public FloatRange DepthInCaveSystemRequirement = DefaultDepthInCaveSystemRequirement;

    public bool AllowSettlements;
    public bool AllowSites;

    private List<Predicate<IWorldTileInfo>> _conditions;

    private List<Predicate<IWorldTileInfo>> BuildRequirements()
    {
        var conditions = new List<Predicate<IWorldTileInfo>>
        {
            info => CheckHilliness(HillinessRequirement, info.Hilliness),
            info => ElevationRequirement.Includes(info.Elevation)
        };

        if (AvgTemperatureRequirement != DefaultAvgTemperatureRequirement)
            conditions.Add(info => AvgTemperatureRequirement.Includes(info.Temperature));
        if (RainfallRequirement != DefaultRainfallRequirement)
            conditions.Add(info => RainfallRequirement.Includes(info.Rainfall));
        if (SwampinessRequirement != DefaultSwampinessRequirement)
            conditions.Add(info => SwampinessRequirement.Includes(info.Swampiness));
        if (BiomeTransitionsRequirement != DefaultBiomeTransitionsRequirement)
            conditions.Add(info => BiomeTransitionsRequirement.Includes(info.BorderingBiomesCount()));
        if (TopologyValueRequirement != DefaultTopologyValueRequirement)
            conditions.Add(info => TopologyValueRequirement.Includes(info.TopologyValue));
        if (DepthInCaveSystemRequirement != DefaultDepthInCaveSystemRequirement)
            conditions.Add(info => DepthInCaveSystemRequirement.Includes(info.DepthInCaveSystem));

        if (RiverRequirement.max <= 0f)
            conditions.Add(info => info.MainRiver == null);
        if (RoadRequirement.max <= 0f)
            conditions.Add(info => info.MainRoad == null);

        bool River(IWorldTileInfo info) => RiverRequirement.Includes(info.MainRiverSize());
        bool Road(IWorldTileInfo info) => RoadRequirement.Includes(info.MainRoadSize());

        if (RiverRequirement.min > 0f && RoadRequirement.min > 0f)
            conditions.Add(info => River(info) || Road(info));
        else if (RiverRequirement.min > 0f)
            conditions.Add(River);
        else if (RoadRequirement.min > 0f)
            conditions.Add(Road);

        if (!AllowSettlements)
            conditions.Add(info => info.WorldObject is not Settlement { Faction.IsPlayer: false });
        if (!AllowSites)
            conditions.Add(info => info.WorldObject is null or Settlement or { Faction.IsPlayer: true });

        return conditions;
    }

    public bool CheckRequirements(IWorldTileInfo worldTile, bool lenient)
    {
        if (!Topology.IsCompatible(worldTile.Topology, lenient)) return false;

        foreach (var condition in _conditions)
            if (!condition(worldTile))
                return false;

        return true;
    }

    protected override void DoWindowContents(LayoutRect layout)
    {
        LunarGUI.LabelDouble(layout, "GeologicalLandforms.Settings.Landform.Commonness".Translate(), Commonness.ToString("F2"));
        LunarGUI.Slider(layout, ref Commonness, 0f, 1f);

        layout.PushEnabled(!Landform.IsLayer);

        LunarGUI.LabelDouble(layout, "GeologicalLandforms.Settings.Landform.CaveChance".Translate(), CaveChance.ToString("F2"));
        LunarGUI.Slider(layout, ref CaveChance, 0f, 1f);

        layout.PopEnabled();
        layout.Abs(10f);

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.3f), "GeologicalLandforms.Settings.Landform.Topology".Translate());
        LunarGUI.Dropdown(layout, Topology, e => Topology = e, "GeologicalLandforms.Settings.Landform.Topology");
        layout.End();

        layout.Abs(20f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.HillinessRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref HillinessRequirement, 1f, 6f, LabelForHilliness);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.TopologyValueRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref TopologyValueRequirement, -1f, 1f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.RoadRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref RoadRequirement, 0f, 1f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.RiverRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref RiverRequirement, 0f, 1f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.ElevationRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref ElevationRequirement, -1000f, 5000f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.AvgTemperatureRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref AvgTemperatureRequirement, -100f, 100f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.RainfallRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref RainfallRequirement, 0f, 5000f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.SwampinessRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref SwampinessRequirement, 0f, 1f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.BiomeTransitionsRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref BiomeTransitionsRequirement, 0f, 6f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.DepthInCaveSystemRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref DepthInCaveSystemRequirement, 0f, 10f);

        layout.Abs(20f);

        LunarGUI.Checkbox(layout, ref AllowSettlements, "GeologicalLandforms.Settings.Landform.AllowSettlements".Translate());

        layout.Abs(10f);

        LunarGUI.Checkbox(layout, ref AllowSites, "GeologicalLandforms.Settings.Landform.AllowSites".Translate());

        if (GUI.changed) _conditions = BuildRequirements();
    }

    private string LabelForHilliness(float val)
    {
        if (val > 5f) return Hilliness.Impassable.GetLabelCap();
        var hilliness = (Hilliness) (int) Math.Floor(val);
        if (hilliness == Hilliness.Impassable) hilliness = Hilliness.Mountainous;
        return hilliness.GetLabelCap();
    }

    private bool CheckHilliness(FloatRange range, Hilliness hilliness)
    {
        if (hilliness == Hilliness.Impassable) return range.max > 5f;
        return range.Includes((float) hilliness);
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

        _conditions = BuildRequirements();
    }

    protected override void OnDelete()
    {
        if (Landform.WorldTileReq == this) Landform.WorldTileReq = null;
    }
}

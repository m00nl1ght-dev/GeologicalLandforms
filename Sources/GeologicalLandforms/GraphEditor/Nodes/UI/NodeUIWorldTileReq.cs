using System;
using System.Collections.Generic;
using LunarFramework.GUI;
using NodeEditorFramework;
using UnityEngine;
using Verse;
using static GeologicalLandforms.Topology;
using static GeologicalLandforms.WorldTileConditions;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World Tile Requirements", 0)]
public class NodeUIWorldTileReq : NodeUIBase
{
    public const string ID = "worldTileReq";
    public override string GetID => ID;

    public override string Title => "World Tile Requirements";
    public override Vector2 DefaultSize => new(400, 800);

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

    public FloatRange HillinessRequirement = DefaultHillinessRequirement;
    public FloatRange RoadRequirement = DefaultRoadRequirement;
    public FloatRange RiverRequirement = DefaultRiverRequirement;
    public FloatRange ElevationRequirement = DefaultElevationRequirement;
    public FloatRange AvgTemperatureRequirement = DefaultAvgTemperatureRequirement;
    public FloatRange RainfallRequirement = DefaultRainfallRequirement;
    public FloatRange SwampinessRequirement = DefaultSwampinessRequirement;
    public FloatRange MapSizeRequirement = DefaultMapSizeRequirement;
    public FloatRange BiomeTransitionsRequirement = DefaultBiomeTransitionsRequirement;

    public bool AllowSettlements;
    public bool AllowSites;

    private List<Condition> _conditions;

    private List<Condition> BuildRequirements()
    {
        var conditions = new List<Condition>();

        if (HillinessRequirement != DefaultHillinessRequirement) conditions.Add(Hilliness(HillinessRequirement));
        if (ElevationRequirement != DefaultElevationRequirement) conditions.Add(Elevation(ElevationRequirement));
        if (AvgTemperatureRequirement != DefaultAvgTemperatureRequirement) conditions.Add(Temperature(AvgTemperatureRequirement));
        if (RainfallRequirement != DefaultRainfallRequirement) conditions.Add(Rainfall(RainfallRequirement));
        if (SwampinessRequirement != DefaultSwampinessRequirement) conditions.Add(Swampiness(SwampinessRequirement));
        if (BiomeTransitionsRequirement != DefaultBiomeTransitionsRequirement) conditions.Add(BorderingBiomes(BiomeTransitionsRequirement));
        if (MapSizeRequirement != DefaultMapSizeRequirement) conditions.Add(ExpectedMapSize(MapSizeRequirement));

        if (RiverRequirement.max <= 0f) conditions.Add(River(RiverRequirement));
        if (RoadRequirement.max <= 0f) conditions.Add(Road(RoadRequirement));

        if (RiverRequirement.min > 0f && RoadRequirement.min > 0f)
            conditions.Add(AnyOf(new List<Condition> { River(RiverRequirement), Road(RoadRequirement) }));
        else if (RiverRequirement.min > 0f)
            conditions.Add(River(RiverRequirement));
        else if (RoadRequirement.min > 0f)
            conditions.Add(Road(RoadRequirement));

        if (!AllowSettlements) conditions.Add(Settlement(false));
        if (!AllowSites) conditions.Add(QuestSite(false));

        return conditions;
    }

    public bool CheckRequirements(IWorldTileInfo worldTile, bool lenient)
    {
        _conditions ??= BuildRequirements();

        if (!Topology.IsCompatible(worldTile.Topology, lenient)) return false;

        foreach (var condition in _conditions)
            if (!condition(worldTile))
                return false;

        return true;
    }

    public bool CheckMapRequirements(IntVec2 mapSize)
    {
        if (!MapSizeRequirement.Includes(mapSize.x)) return false;
        if (!MapSizeRequirement.Includes(mapSize.z)) return false;
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
        LunarGUI.RangeSlider(layout, ref HillinessRequirement, 1f, 5f);

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

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.MapSizeRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref MapSizeRequirement, 50f, 1000f);

        layout.Abs(10f);

        LunarGUI.LabelCentered(layout, "GeologicalLandforms.Settings.Landform.BiomeTransitionsRequirement".Translate());
        LunarGUI.RangeSlider(layout, ref BiomeTransitionsRequirement, 0f, 6f);

        layout.Abs(20f);

        LunarGUI.Checkbox(layout, ref AllowSettlements, "GeologicalLandforms.Settings.Landform.AllowSettlements".Translate());

        layout.Abs(10f);

        LunarGUI.Checkbox(layout, ref AllowSites, "GeologicalLandforms.Settings.Landform.AllowSites".Translate());

        if (GUI.changed) _conditions = null;
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
    }
}

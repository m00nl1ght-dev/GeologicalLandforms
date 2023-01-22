using System;
using LunarFramework.GUI;
using NodeEditorFramework;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static GeologicalLandforms.Topology;

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

    public FloatRange HillinessRequirement = new(1f, 5f);
    public FloatRange RoadRequirement = new(0f, 1f);
    public FloatRange RiverRequirement = new(0f, 1f);
    public FloatRange ElevationRequirement = new(0f, 5000f);
    public FloatRange AvgTemperatureRequirement = new(-100f, 100f);
    public FloatRange RainfallRequirement = new(0f, 5000f);
    public FloatRange SwampinessRequirement = new(0f, 1f);
    public FloatRange MapSizeRequirement = new(250f, 1000f);
    public FloatRange BiomeTransitionsRequirement = new(0f, 6f);
    
    public bool AllowSettlements;
    public bool AllowSites;
    
    // private List<Predicate<WorldTileInfo>> _requirements = new(); // TODO
    
    public bool CheckRequirements(IWorldTileInfo worldTile, bool lenientTopology)
    {
        if (!Topology.IsCompatible(worldTile.Topology, lenientTopology)) return false;
        if (!HillinessRequirement.Includes((float) worldTile.Hilliness)) return false;
        if (!ElevationRequirement.Includes(worldTile.Elevation)) return false;
        if (!AvgTemperatureRequirement.Includes(worldTile.Temperature)) return false;
        if (!RainfallRequirement.Includes(worldTile.Rainfall) 
            && !(RainfallRequirement.max == 5000 && worldTile.Rainfall > 5000f)) return false;
        if (!SwampinessRequirement.Includes(worldTile.Swampiness)) return false;
        if (!BiomeTransitionsRequirement.Includes(worldTile.BorderingBiomes?.Count ?? 0)) return false;

        var mapParent = worldTile.WorldObject;
        bool isPlayer = mapParent?.Faction is { IsPlayer: true };
        if (!AllowSettlements && mapParent is Settlement && !isPlayer) return false;
        if (!AllowSites && mapParent is Site && !isPlayer) return false;

        var expectedMapSize = mapParent is Site site ? site.PreferredMapSize : Find.World.info.initialMapSize;
        int expectedMapSizeInt = Math.Min(expectedMapSize.x, expectedMapSize.z);
        if (!MapSizeRequirement.Includes(expectedMapSizeInt)) return false;

        float riverWidth = worldTile.MainRiver?.widthOnWorld ?? 0f;
        float mainRoadMultiplier = worldTile.MainRoad?.movementCostMultiplier ?? 1f;
        if (RoadRequirement.max <= 0f && mainRoadMultiplier < 1f) return false;
        if (RiverRequirement.max <= 0f && riverWidth > 0f) return false;
        
        if (!RoadRequirement.Includes(1f - mainRoadMultiplier) && 
            !RiverRequirement.Includes(riverWidth)) return false;

        return true;
    }

    public bool CheckMapRequirements(IntVec2 mapSize)
    {
        if (!MapSizeRequirement.Includes(mapSize.x)) return false;
        if (!MapSizeRequirement.Includes(mapSize.z)) return false;
        return true;
    }

    // TODO evetually refactor this to use WorldTileConditions
    
    protected override void DoWindowContents(LayoutRect layout)
    {
        LunarGUI.LabelDouble(layout, "GeologicalLandforms.Settings.Landform.Commonness".Translate(), Commonness.ToString("F2"));
        LunarGUI.Slider(layout, ref Commonness, 0f, 1f);

        LunarGUI.PushEnabled(!Landform.IsLayer);
        LunarGUI.LabelDouble(layout, "GeologicalLandforms.Settings.Landform.CaveChance".Translate(), CaveChance.ToString("F2"));
        LunarGUI.Slider(layout, ref CaveChance, 0f, 1f);
        LunarGUI.PopEnabled();
        
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
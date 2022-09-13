using System;
using System.Globalization;
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
    public override Vector2 DefaultSize => new(400, 875);
    
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
    
    public bool AllowSettlements;
    public bool AllowSites;
    
    public bool CheckRequirements(IWorldTileInfo worldTile, bool lenientTopology)
    {
        if (!IsTopologyCompatible(Topology, worldTile.Topology, lenientTopology)) return false;
        if (!HillinessRequirement.Includes((float) worldTile.Hilliness)) return false;
        if (!ElevationRequirement.Includes(worldTile.Elevation)) return false;
        if (!AvgTemperatureRequirement.Includes(worldTile.Temperature)) return false;
        if (!RainfallRequirement.Includes(worldTile.Rainfall) 
            && !(RainfallRequirement.max == 5000 && worldTile.Rainfall > 5000f)) return false;
        if (!SwampinessRequirement.Includes(worldTile.Swampiness)) return false;

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

    public static bool IsTopologyCompatible(Topology req, Topology tile, bool lenient)
    {
        if (req == Any || tile == req) return true;
        if (!lenient) return false;
        if (req.IsCoast() && tile.IsCoast(true)) return true;
        if (req.IsCliff() && tile.IsCliff(true)) return true;
        if (req == Inland && tile.IsCliff(true)) return true;
        return false;
    }

    public bool CheckMapRequirements(int mapSize)
    {
        if (!MapSizeRequirement.Includes(mapSize)) return false;
        
        return true;
    }

    protected override void DoWindowContents(Listing_Standard listing)
    {
        GuiUtils.CenteredLabel(listing, "GeologicalLandforms.Settings.Landform.Commonness".Translate(), Math.Round(Commonness, 2).ToString(CultureInfo.InvariantCulture));
        Commonness = listing.Slider(Commonness, 0f, 1f);

        GUI.enabled = !Landform.IsLayer;
        
        GuiUtils.CenteredLabel(listing, "GeologicalLandforms.Settings.Landform.CaveChance".Translate(), Math.Round(CaveChance, 2).ToString(CultureInfo.InvariantCulture));
        CaveChance = listing.Slider(CaveChance, 0f, 1f);
        listing.Gap(18f);
        
        GUI.enabled = true;
        
        GuiUtils.Dropdown(listing, "GeologicalLandforms.Settings.Landform.Topology".Translate(), Topology, e => Topology = e, 150f, "GeologicalLandforms.Settings.Landform.Topology");
        listing.Gap();
        
        GuiUtils.FloatRangeSlider(listing, ref HillinessRequirement, "GeologicalLandforms.Settings.Landform.HillinessRequirement".Translate(), 1f, 5f);
        GuiUtils.FloatRangeSlider(listing, ref RoadRequirement, "GeologicalLandforms.Settings.Landform.RoadRequirement".Translate(), 0f, 1f);
        GuiUtils.FloatRangeSlider(listing, ref RiverRequirement, "GeologicalLandforms.Settings.Landform.RiverRequirement".Translate(), 0f, 1f);
        GuiUtils.FloatRangeSlider(listing, ref ElevationRequirement, "GeologicalLandforms.Settings.Landform.ElevationRequirement".Translate(), -1000f, 5000f);
        GuiUtils.FloatRangeSlider(listing, ref AvgTemperatureRequirement, "GeologicalLandforms.Settings.Landform.AvgTemperatureRequirement".Translate(), -100f, 100f);
        GuiUtils.FloatRangeSlider(listing, ref RainfallRequirement, "GeologicalLandforms.Settings.Landform.RainfallRequirement".Translate(), 0f, 5000f);
        GuiUtils.FloatRangeSlider(listing, ref SwampinessRequirement, "GeologicalLandforms.Settings.Landform.SwampinessRequirement".Translate(), 0f, 1f);
        GuiUtils.FloatRangeSlider(listing, ref MapSizeRequirement, "GeologicalLandforms.Settings.Landform.MapSizeRequirement".Translate(), 50f, 1000f);
        listing.Gap();
        
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.AllowSettlements".Translate(), ref AllowSettlements);
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.AllowSites".Translate(), ref AllowSites);
        
        if (GUI.changed) TerrainCanvas.OnNodeChange(this);
    }

    public override void DrawNode()
    {
        if (Landform.Id != null) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeUIWorldTileReq existing = Landform.WorldTileReq;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.WorldTileReq = this;
    }
}
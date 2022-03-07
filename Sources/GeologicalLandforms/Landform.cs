using System;
using System.Globalization;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class Landform : IExposable
{
    public string Id;
    public bool IsCustom;
    
    public string DisplayName;
    public bool DisplayNameHasDirection;
    
    public Topology Topology = Topology.Inland;
    public float Commonness = 1f;

    public FloatRange HillinessRequirement = new(1f, 5f);
    public FloatRange RoadRequirement = new(0f, 1f);
    public FloatRange RiverRequirement = new(0f, 1f);
    public FloatRange ElevationRequirement = new(0f, 5000f);
    public FloatRange AvgTemperatureRequirement = new(-100f, 100f);
    public FloatRange RainfallRequirement = new(0f, 5000f);
    public FloatRange SwampinessRequirement = new(0f, 1f);
    public FloatRange MapSizeRequirement = new(250f, 1000f);
    
    public bool AllowCaves = true;
    public bool RequireCaves;
    public bool AllowSettlements;
    public bool AllowSites;

    public GenNoiseConfig GenConfig = new();
    
    public string TranslatedName => DisplayName?.Length > 0 ? DisplayName : ("GeologicalLandforms.Landform." + Id).Translate();
    
    public void ExposeData()
    {
        Scribe_Values.Look(ref Id, "Id");
        Scribe_Values.Look(ref IsCustom, "IsCustom");
        Scribe_Values.Look(ref DisplayName, "DisplayName");
        Scribe_Values.Look(ref DisplayNameHasDirection, "DisplayNameHasDirection");
        Scribe_Values.Look(ref Topology, "Topology", Topology.Inland);
        Scribe_Values.Look(ref Commonness, "Commonness", 1f);
        Scribe_Values.Look(ref HillinessRequirement, "HillinessRequirement", new(1f, 5f));
        Scribe_Values.Look(ref RoadRequirement, "RoadRequirement", new(0f, 1f));
        Scribe_Values.Look(ref RiverRequirement, "RiverRequirement", new(0f, 1f));
        Scribe_Values.Look(ref ElevationRequirement, "ElevationRequirement", new(0f, 5000f));
        Scribe_Values.Look(ref AvgTemperatureRequirement, "AvgTemperatureRequirement", new(-100f, 100f));
        Scribe_Values.Look(ref RainfallRequirement, "RainfallRequirement", new(0f, 5000f));
        Scribe_Values.Look(ref SwampinessRequirement, "SwampinessRequirement", new(0f, 1f));
        Scribe_Values.Look(ref MapSizeRequirement, "MapSizeRequirement", new(250f, 1000f));
        Scribe_Values.Look(ref AllowCaves, "AllowCaves", true);
        Scribe_Values.Look(ref RequireCaves, "RequireCaves");
        Scribe_Values.Look(ref AllowSettlements, "AllowBuildings");
        Scribe_Values.Look(ref AllowSites, "AllowSites");
        Scribe_Deep.Look(ref GenConfig, "GenConfig");
    }

    public bool CheckConditions(WorldTileInfo worldTile)
    {
        if (Topology != Topology.Any && worldTile.Topology != Topology) return false;
        if (!HillinessRequirement.Includes((float) worldTile.Tile.hilliness)) return false;
        if (!ElevationRequirement.Includes(worldTile.Tile.elevation)) return false;
        if (!AvgTemperatureRequirement.Includes(worldTile.Tile.temperature)) return false;
        if (!RainfallRequirement.Includes(worldTile.Tile.rainfall)) return false;
        if (!SwampinessRequirement.Includes(worldTile.Tile.swampiness)) return false;
        if (!AllowCaves && worldTile.World.HasCaves(worldTile.TileId)) return false;
        if (RequireCaves && !worldTile.World.HasCaves(worldTile.TileId)) return false;

        MapParent mapParent = Find.WorldObjects.MapParentAt(worldTile.TileId);
        if (!AllowSettlements && mapParent is Settlement) return false;
        if (!AllowSites && mapParent is Site) return false;

        IntVec3 expectedMapSize = mapParent is Site site ? site.PreferredMapSize : Find.World.info.initialMapSize;
        int expectedMapSizeInt = Math.Min(expectedMapSize.x, expectedMapSize.z);
        if (!MapSizeRequirement.Includes(expectedMapSizeInt)) return false;

        if (RoadRequirement.max <= 0f && worldTile.MainRoadMultiplier < 1f) return false;
        if (RiverRequirement.max <= 0f && worldTile.RiverWidth > 0f) return false;
        
        if (!RoadRequirement.Includes(1f - worldTile.MainRoadMultiplier) && 
            !RiverRequirement.Includes(worldTile.RiverWidth)) return false;
        
        return true;
    }

    public void DoSettingsWindowContents(Listing_Standard listing)
    {
        if (IsCustom)
        {
            string oldId = Id;
            
            Id = Settings.TextEntry(listing, "GeologicalLandforms.Settings.Landform.Id".Translate(), Id);
            DisplayName = Settings.TextEntry(listing, "GeologicalLandforms.Settings.Landform.DisplayName".Translate(), DisplayName);
            listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.DisplayNameHasDirection".Translate(), ref DisplayNameHasDirection);
            listing.Gap();

            if (Id != oldId)
            {
                if (Main.Settings.CustomLandforms.ContainsKey(Id))
                {
                    Id = "New" + Id;
                }
                else
                {
                    Main.Settings.CustomLandforms.Remove(oldId);
                    Main.Settings.CustomLandforms.Add(Id, this);
                }
            }
        }

        Settings.Dropdown(listing, "GeologicalLandforms.Settings.Landform.Topology".Translate(), Topology, e => Topology = e, 200f, "GeologicalLandforms.Settings.Landform.Topology");
        Settings.CenteredLabel(listing, "GeologicalLandforms.Settings.Landform.Commonness".Translate(), Math.Round(Commonness, 2).ToString(CultureInfo.InvariantCulture));
        Commonness = listing.Slider(Commonness, 0f, 1f);
        listing.Gap(18f);
        
        Settings.FloatRangeSlider(listing, ref HillinessRequirement, "GeologicalLandforms.Settings.Landform.HillinessRequirement".Translate(), 1f, 5f);
        Settings.FloatRangeSlider(listing, ref RoadRequirement, "GeologicalLandforms.Settings.Landform.RoadRequirement".Translate(), 0f, 1f);
        Settings.FloatRangeSlider(listing, ref RiverRequirement, "GeologicalLandforms.Settings.Landform.RiverRequirement".Translate(), 0f, 1f);
        Settings.FloatRangeSlider(listing, ref ElevationRequirement, "GeologicalLandforms.Settings.Landform.ElevationRequirement".Translate(), -1000f, 5000f);
        Settings.FloatRangeSlider(listing, ref AvgTemperatureRequirement, "GeologicalLandforms.Settings.Landform.AvgTemperatureRequirement".Translate(), -100f, 100f);
        Settings.FloatRangeSlider(listing, ref RainfallRequirement, "GeologicalLandforms.Settings.Landform.RainfallRequirement".Translate(), 0f, 5000f);
        Settings.FloatRangeSlider(listing, ref SwampinessRequirement, "GeologicalLandforms.Settings.Landform.SwampinessRequirement".Translate(), 0f, 1f);
        Settings.FloatRangeSlider(listing, ref MapSizeRequirement, "GeologicalLandforms.Settings.Landform.MapSizeRequirement".Translate(), 50f, 1000f);
        listing.Gap(18f);
        
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.AllowSettlements".Translate(), ref AllowSettlements);
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.AllowSites".Translate(), ref AllowSites);
        listing.Gap();
        
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.AllowCaves".Translate(), ref AllowCaves);
        if (AllowCaves) listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.RequireCaves".Translate(), ref RequireCaves);
        listing.Gap(24f);
        
        GenConfig.DoSettingsWindowContents(listing);
    }
}
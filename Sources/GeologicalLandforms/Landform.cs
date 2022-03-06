using System;
using System.Globalization;
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
    
    public bool AllowCaves = true;
    public bool RequireCaves;

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
        Scribe_Values.Look(ref AllowCaves, "AllowCaves", true);
        Scribe_Values.Look(ref RequireCaves, "RequireCaves");
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
            
            Id = Settings.TextEntry(listing, "Id", Id);
            DisplayName = Settings.TextEntry(listing, "DisplayName", DisplayName);
            listing.CheckboxLabeled("DisplayNameHasDirection", ref DisplayNameHasDirection);
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

        Settings.Dropdown(listing, "Topology", Topology, e => Topology = e);
        Settings.CenteredLabel(listing, "Commonness", Math.Round(Commonness, 2).ToString(CultureInfo.InvariantCulture));
        Commonness = listing.Slider(Commonness, 0f, 1f);
        listing.Gap(18f);
        
        Settings.FloatRangeSlider(listing, ref HillinessRequirement, "HillinessRequirement", 1f, 5f);
        Settings.FloatRangeSlider(listing, ref RoadRequirement, "RoadRequirement", 0f, 1f);
        Settings.FloatRangeSlider(listing, ref RiverRequirement, "RiverRequirement", 0f, 1f);
        Settings.FloatRangeSlider(listing, ref ElevationRequirement, "ElevationRequirement", -1000f, 5000f);
        Settings.FloatRangeSlider(listing, ref AvgTemperatureRequirement, "AvgTemperatureRequirement", -100f, 100f);
        Settings.FloatRangeSlider(listing, ref RainfallRequirement, "RainfallRequirement", 0f, 5000f);
        Settings.FloatRangeSlider(listing, ref SwampinessRequirement, "SwampinessRequirement", 0f, 1f);
        listing.Gap(18f);
        
        listing.CheckboxLabeled("AllowCaves", ref AllowCaves);
        if (AllowCaves) listing.CheckboxLabeled("RequireCaves", ref RequireCaves);
        listing.Gap(24f);
        
        GenConfig.DoSettingsWindowContents(listing);
    }
}
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class Settings : ModSettings
{
    public int MaxLandformSearchRadius = 100;

    public bool EnableCellFinderOptimization = true;
    
    public bool ShowWorldTileDebugInfo;

    private static Vector2 _scrollPos = Vector2.zero;

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect rect = new(0.0f, 0.0f, inRect.width, 300f);
        rect.xMax *= 0.95f;
        
        Listing_Standard listingStandard = new();
        listingStandard.Begin(rect);
        GUI.EndGroup();
        Widgets.BeginScrollView(inRect, ref _scrollPos, rect);

        if (listingStandard.ButtonText("GeologicalLandforms.Settings.OpenEditor".Translate()))
        {
            Find.WindowStack.Add(new LandformGraphEditor());
        }

        if (listingStandard.ButtonText("GeologicalLandforms.Settings.OpenDataDir".Translate()))
        {
            Application.OpenURL(LandformManager.CustomLandformsDir(LandformManager.CurrentVersion));
        }
            
        if (listingStandard.ButtonText("GeologicalLandforms.Settings.ResetAll".Translate()))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmResetAll".Translate(), ResetAll));
        }
        
        if (Prefs.DevMode && Find.CurrentMap != null && listingStandard.ButtonText("[dev] Replace all stone on current map"))
        {
            List<FloatMenuOption> options = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsNonResourceNaturalRock)
                .Select(e => new FloatMenuOption(e.defName, () => ReplaceNaturalRock(e))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        listingStandard.Gap();
        
        GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.MaxLandformSearchRadius".Translate(), MaxLandformSearchRadius.ToString(CultureInfo.InvariantCulture));
        MaxLandformSearchRadius = (int) listingStandard.Slider(MaxLandformSearchRadius, 10f, 500f);
        
        listingStandard.Gap();
        listingStandard.CheckboxLabeled("GeologicalLandforms.Settings.EnableCellFinderOptimization".Translate(), ref EnableCellFinderOptimization);

        if (Prefs.DevMode)
        {
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("GeologicalLandforms.Settings.ShowWorldTileDebugInfo".Translate(), ref ShowWorldTileDebugInfo);
        }

        Widgets.EndScrollView();
        
        RimWorld_Misc.RunOnMainMenuNow();
    }

    private void ReplaceNaturalRock(ThingDef thingDef)
    {
        Map map = Find.CurrentMap;
        map.regionAndRoomUpdater.Enabled = false;

        TerrainDef terrainDef = thingDef.building.naturalTerrain;
            
        foreach (IntVec3 allCell in map.AllCells)
        {
            if (map.edificeGrid[allCell]?.def?.IsNonResourceNaturalRock ?? false)
                GenSpawn.Spawn(thingDef, allCell, map);
                
            if (map.terrainGrid.TerrainAt(allCell)?.smoothedTerrain != null)
            {
                map.terrainGrid.SetTerrain(allCell, terrainDef);
            }
        }
            
        map.regionAndRoomUpdater.Enabled = true;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxLandformSearchRadius, "MaxLandformSearchRadius", 100);
        Scribe_Values.Look(ref EnableCellFinderOptimization, "EnableCellFinderOptimization", true);
        Scribe_Values.Look(ref ShowWorldTileDebugInfo, "ShowWorldTileDebugInfo");
        base.ExposeData();
    }
    
    public void ResetAll()
    {
        LandformManager.ResetAll();
        ShowWorldTileDebugInfo = false;
        EnableCellFinderOptimization = true;
        MaxLandformSearchRadius = 100;
    }
}
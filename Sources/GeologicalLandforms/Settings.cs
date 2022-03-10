using System.Globalization;
using GeologicalLandforms.TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class Settings : ModSettings
{
    public int MaxLandformSearchRadius = 50;
    
    public bool UseCustomConfig;

    private static Vector2 _scrollPos = Vector2.zero;

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect rect = new(0.0f, 0.0f, inRect.width, 300f);
        rect.xMax *= 0.95f;
        
        Listing_Standard listingStandard = new();
        listingStandard.Begin(rect);
        GUI.EndGroup();
        Widgets.BeginScrollView(inRect, ref _scrollPos, rect);

        GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.MaxLandformSearchRadius".Translate(), MaxLandformSearchRadius.ToString(CultureInfo.InvariantCulture));
        MaxLandformSearchRadius = (int) listingStandard.Slider(MaxLandformSearchRadius, 10f, 500f);

        listingStandard.CheckboxLabeled("GeologicalLandforms.Settings.Advanced".Translate(), ref UseCustomConfig);
        if (UseCustomConfig)
        {
            listingStandard.Gap();
            
            if (listingStandard.ButtonText("GeologicalLandforms.Settings.OpenEditor".Translate()))
            {
                Find.WindowStack.Add(new LandformGraphEditor());
            }

            if (listingStandard.ButtonText("GeologicalLandforms.Settings.OpenDataDir".Translate()))
            {
                Application.OpenURL(LandformManager.CustomLandformsDir);
            }
            
            if (listingStandard.ButtonText("GeologicalLandforms.Settings.ResetAll".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmResetAll".Translate(), ResetAll));
            }
        }

        Widgets.EndScrollView();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref UseCustomConfig, "UseCustomConfig");
        Scribe_Values.Look(ref MaxLandformSearchRadius, "MaxLandformSearchRadius", 50);
        base.ExposeData();
    }
    
    public void ResetAll()
    {
        LandformManager.ResetAll();
        UseCustomConfig = false;
        MaxLandformSearchRadius = 50;
    }
}
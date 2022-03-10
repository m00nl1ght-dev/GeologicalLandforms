using System;
using System.Collections.Generic;
using System.Globalization;
using Verse;

namespace GeologicalLandforms;

public class GenNoiseConfig : IExposable
{
    public Dictionary<NoiseType, GenNoiseStack> NoiseStacks = new();

    public float ThresholdShallow = 0.1f;
    public float ThresholdBeach = 0.45f;

    public string TerrainDeep;
    public string TerrainShallow;
    public string TerrainBeach;
    
    public float HillModifierEffectiveness = 1f;
    public float MaxElevationIfWaterCovered;
    
    private static NoiseType SelectedNoiseType;
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref NoiseStacks, "NoiseStacks", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref ThresholdShallow, "ThresholdShallow", 0.1f);
        Scribe_Values.Look(ref ThresholdBeach, "ThresholdBeach", 0.45f);
        Scribe_Values.Look(ref TerrainDeep, "TerrainDeep");
        Scribe_Values.Look(ref TerrainShallow, "TerrainShallow");
        Scribe_Values.Look(ref TerrainBeach, "TerrainBeach");
        Scribe_Values.Look(ref HillModifierEffectiveness, "HillModifierEffectiveness", 1f);
        Scribe_Values.Look(ref MaxElevationIfWaterCovered, "MaxElevationIfWaterCovered");
    }

    public void DoSettingsWindowContents(Listing_Standard listingStandard)
    {
        GuiUtils.Dropdown(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.SelectNoiseType".Translate(), 
            SelectedNoiseType, e => SelectedNoiseType = e, 200f, "GeologicalLandforms.Settings.GenNoiseConfig.NoiseType");
        
        listingStandard.Gap(18f);
        if (SelectedNoiseType == NoiseType.Coast)
        {
            GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.ThresholdShallow".Translate(), Math.Round(ThresholdShallow, 2).ToString(CultureInfo.InvariantCulture));
            ThresholdShallow = listingStandard.Slider(ThresholdShallow, -1f, 1f);
            GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.ThresholdBeach".Translate(), Math.Round(ThresholdBeach, 2).ToString(CultureInfo.InvariantCulture));
            ThresholdBeach = listingStandard.Slider(ThresholdBeach, -1f, 1f);
            listingStandard.Gap();
            
            TerrainDeep = GuiUtils.TextEntry(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.TerrainDeep".Translate(), TerrainDeep);
            TerrainShallow = GuiUtils.TextEntry(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.TerrainShallow".Translate(), TerrainShallow);
            TerrainBeach = GuiUtils.TextEntry(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.TerrainBeach".Translate(), TerrainBeach);
            listingStandard.Gap(18f);
        }

        if (SelectedNoiseType == NoiseType.Elevation)
        {
            GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.HillModifierEffectiveness".Translate(), Math.Round(HillModifierEffectiveness, 2).ToString(CultureInfo.InvariantCulture));
            HillModifierEffectiveness = listingStandard.Slider(HillModifierEffectiveness, 0f, 2f);
            GuiUtils.CenteredLabel(listingStandard, "GeologicalLandforms.Settings.GenNoiseConfig.MaxElevationIfWaterCovered".Translate(), Math.Round(MaxElevationIfWaterCovered, 2).ToString(CultureInfo.InvariantCulture));
            MaxElevationIfWaterCovered = listingStandard.Slider(MaxElevationIfWaterCovered, 0f, 2f);
            listingStandard.Gap(18f);
        }
        
        NoiseStacks.TryGetValue(SelectedNoiseType, out GenNoiseStack noiseStack);
        if (noiseStack == null) NoiseStacks.Add(SelectedNoiseType, noiseStack = new GenNoiseStack(SelectedNoiseType));
        noiseStack.DoSettingsWindowContents(listingStandard, SelectedNoiseType);
    }

    public enum NoiseType
    {
        Coast, Elevation, Fertility
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms;

public class GenNoiseLayer : IExposable
{
    public HashSet<GenNoiseStack.MapSide> MapSides = new();
    public GenNoiseStack.CombineMethod SideCombineMethod = GenNoiseStack.CombineMethod.Min;
    public GenNoiseStack.CombineMethod LayerCombineMethod = GenNoiseStack.CombineMethod.Min;

    public FloatRange SpanPositiveX = new(0f, 0f);
    public FloatRange SpanNegativeX = new(0f, 0f);
    public FloatRange SpanPositiveZ = new(0f, 0f);
    public FloatRange SpanNegativeZ = new(0f, 0f);
    public FloatRange CenterX = new(0f, 0f);
    public FloatRange CenterZ = new(0f, 0f);
    public FloatRange RotationOffset = new(0f, 0f);
    public FloatRange Bias = new(0f, 0f);
    public FloatRange Clamp = new(-1f, 1f);

    public float ApplyChance = 1f;
    
    public bool InvertX;
    public bool InvertZ;
    
    public bool Radial;
    public bool SyncPosNeg = true;
    
    public bool AlignWithRiver;
    public bool AlignWithMainRoad;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref MapSides, "MapSides", LookMode.Value);
        Scribe_Values.Look(ref SideCombineMethod, "SideCombineMethod");
        Scribe_Values.Look(ref LayerCombineMethod, "LayerCombineMethod");
        Scribe_Values.Look(ref SpanPositiveX, "SpanPositiveX");
        Scribe_Values.Look(ref SpanNegativeX, "SpanNegativeX");
        Scribe_Values.Look(ref SpanPositiveZ, "SpanPositiveZ");
        Scribe_Values.Look(ref SpanNegativeZ, "SpanNegativeZ");
        Scribe_Values.Look(ref CenterX, "CenterX");
        Scribe_Values.Look(ref CenterZ, "CenterZ");
        Scribe_Values.Look(ref RotationOffset, "RotationOffset");
        Scribe_Values.Look(ref Bias, "Bias");
        Scribe_Values.Look(ref Clamp, "Clamp", new(-1f, 1f));
        Scribe_Values.Look(ref ApplyChance, "ApplyChance", 1f);
        Scribe_Values.Look(ref InvertX, "InvertX");
        Scribe_Values.Look(ref InvertZ, "InvertZ");
        Scribe_Values.Look(ref Radial, "Radial");
        Scribe_Values.Look(ref SyncPosNeg, "SyncPosNeg", true);
        Scribe_Values.Look(ref AlignWithRiver, "AlignWithRiver");
        Scribe_Values.Look(ref AlignWithMainRoad, "AlignWithMainRoad");
    }
    
    public GenNoiseLayer() {}

    public GenNoiseLayer(GenNoiseConfig.NoiseType noiseType, GenNoiseStack.CombineMethod applyMethod)
    {
        SetDefaults(noiseType, applyMethod);
    }

    public ModuleBase BuildModule(Map map)
    {
        var spanPX = SpanPositiveX.RandomInRange;
        var spanPZ = SpanPositiveZ.RandomInRange;
        ModuleBase dist = new BiasedDistFromXZ(
            (InvertX ? 1f : -1f) * spanPX, 
            (InvertX ? 1f : -1f) * (SyncPosNeg ? spanPX : SpanNegativeX.RandomInRange), 
            (InvertZ ? 1f : -1f) * spanPZ, 
            (InvertZ ? 1f : -1f) * (SyncPosNeg ? spanPZ : SpanNegativeZ.RandomInRange), 
            CenterX.RandomInRange * map.Size.x, 
            CenterZ.RandomInRange * map.Size.z, 
            Bias.RandomInRange, Radial);
            
        return new Clamp(Clamp.min, Clamp.max, dist);
    }
    
    public void DoSettingsWindowContents(Listing_Standard listingStandard, int idx)
    {
        Settings.CenteredLabel(listingStandard, "", "Layer " + idx);
            
        listingStandard.Gap(24f);
        Settings.FloatRangeSlider(listingStandard, ref CenterX, "CenterX", 0f, 1f);
        Settings.FloatRangeSlider(listingStandard, ref CenterZ, "CenterZ", 0f, 1f);

        if (SyncPosNeg)
        {
            Settings.FloatRangeSlider(listingStandard, ref SpanPositiveX, "SpanX", 0f, 1000f);
            SpanNegativeX = SpanPositiveX;
        }
        else
        {
            Settings.FloatRangeSlider(listingStandard, ref SpanPositiveX, "SpanPositiveX", 0f, 1000f);
            Settings.FloatRangeSlider(listingStandard, ref SpanNegativeX, "SpanNegativeX", 0f, 1000f);
        }
        
        if (SyncPosNeg)
        {
            Settings.FloatRangeSlider(listingStandard, ref SpanPositiveZ, "SpanZ", 0f, 1000f);
            SpanNegativeZ = SpanPositiveZ;
        }
        else
        {
            Settings.FloatRangeSlider(listingStandard, ref SpanPositiveZ, "SpanPositiveZ", 0f, 1000f);
            Settings.FloatRangeSlider(listingStandard, ref SpanNegativeZ, "SpanNegativeZ", 0f, 1000f);
        }

        Settings.FloatRangeSlider(listingStandard, ref RotationOffset, "RotationOffset", -180f, 180f);
        Settings.FloatRangeSlider(listingStandard, ref Bias, "Bias", -2.5f, 2.5f);
        Settings.FloatRangeSlider(listingStandard, ref Clamp, "Clamp", -2.5f, 2.5f);

        listingStandard.Gap();
        Settings.Checkboxes(listingStandard, "Map Side Filter: ", ref MapSides, 100f, 200f);
            
        listingStandard.Gap();
        Settings.RadioButtons(listingStandard, "Side Combine Method: ", ref SideCombineMethod, 100f, 200f);
            
        if (idx > 0)
        {
            listingStandard.Gap();
            Settings.RadioButtons(listingStandard, "Layer Combine Method: ", ref LayerCombineMethod, 100f, 200f);
        }
        
        bool[] options1 = { InvertX, InvertZ, Radial, SyncPosNeg };
            
        listingStandard.Gap();
        Settings.Checkboxes(listingStandard, "Span Options: ", 100f, 200f, ref options1, "InvertX", "InvertZ", "Radial", "SyncPosNeg");
        
        InvertX = options1[0];
        InvertZ = options1[1];
        Radial = options1[2];
        SyncPosNeg = options1[3];
        
        bool[] options2 = { AlignWithRiver, AlignWithMainRoad };
        
        listingStandard.Gap();
        Settings.Checkboxes(listingStandard, "Align Options: ", 100f, 200f, ref options2, "AlignRiver", "AlignRoad");

        AlignWithRiver = options2[0];
        AlignWithMainRoad= options2[1];
        
        listingStandard.Gap();
        Settings.CenteredLabel(listingStandard, "ApplyChance", Math.Round(ApplyChance, 2).ToString(CultureInfo.InvariantCulture));
        ApplyChance = listingStandard.Slider(ApplyChance, 0f, 1f);

        if (Radial)
        {
            InvertZ = InvertX;
        }

        if (SyncPosNeg)
        {
            SpanNegativeX = SpanPositiveX;
            SpanNegativeZ = SpanPositiveZ;
        }
    }

    public void SetDefaults(GenNoiseConfig.NoiseType noiseType, GenNoiseStack.CombineMethod combineMethod)
    {
        if (combineMethod == GenNoiseStack.CombineMethod.Multiply)
        {
            SideCombineMethod = GenNoiseStack.CombineMethod.Multiply;
            LayerCombineMethod = GenNoiseStack.CombineMethod.Multiply;
            Bias = new(1f, 1f);
            Clamp = new(0f, 2.5f);
            return;
        }
        
        if (combineMethod is GenNoiseStack.CombineMethod.Min or GenNoiseStack.CombineMethod.Max)
        {
            SideCombineMethod = combineMethod;
            LayerCombineMethod = combineMethod;
            return;
        }

        if (noiseType == GenNoiseConfig.NoiseType.Coast)
        {
            MapSides = new HashSet<GenNoiseStack.MapSide> { GenNoiseStack.MapSide.Seaside };
            SpanPositiveX = new (20f, 60f);
            SpanNegativeX = new (20f, 60f);
            CenterZ = new (0.5f, 0.5f);
            Bias = new (-1f, -1f);
            Clamp = new (-1f, 2.5f);
            InvertX = true;
            return;
        }
        
        if (noiseType == GenNoiseConfig.NoiseType.Elevation)
        {
            MapSides = new HashSet<GenNoiseStack.MapSide> { GenNoiseStack.MapSide.Landside };
            SpanPositiveX = new (120f, 120f);
            SpanNegativeX = new (120f, 120f);
            CenterZ = new (0.5f, 0.5f);
            Bias = new (1f, 1f);
            Clamp = new (0f, 1f);
        }
    }
}
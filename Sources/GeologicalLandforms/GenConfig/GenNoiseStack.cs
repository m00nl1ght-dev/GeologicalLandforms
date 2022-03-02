using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms;

public class GenNoiseStack : IExposable
{
    public static HashSet<MapSide> MapSidesAll => new HashSet<MapSide>
    {
        MapSide.Landside, MapSide.Seaside, MapSide.Left, MapSide.Right
    };

    public Dictionary<CombineMethod, Entry> Entries = new();

    public class Entry : IExposable
    {
        public List<GenNoiseLayer> Layers = new();
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref Layers, "Layers", LookMode.Deep);
        }
    }

    public double PerlinFrequency = 0.03f;
    public double PerlinLacunarity = 2f;
    public double PerlinPersistence = 0.5f;
    public int PerlinOctaves = 3;
    public int PerlinSeed;
    
    public double BaseScale = 0.5f;
    public double BaseBias = 0.5f;
    
    public double MinSmoothness;
    public double MaxSmoothness;
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref Entries, "Entries", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref PerlinFrequency, "PerlinFrequency", 0.03f);
        Scribe_Values.Look(ref PerlinLacunarity, "PerlinLacunarity", 2f);
        Scribe_Values.Look(ref PerlinPersistence, "PerlinPersistence", 0.5f);
        Scribe_Values.Look(ref PerlinOctaves, "PerlinOctaves", 3);
        Scribe_Values.Look(ref PerlinSeed, "PerlinSeed");
        Scribe_Values.Look(ref BaseScale, "BaseScale", 0.5f);
        Scribe_Values.Look(ref BaseBias, "BaseBias", 0.5f);
        Scribe_Values.Look(ref MinSmoothness, "MinSmoothness");
        Scribe_Values.Look(ref MaxSmoothness, "MaxSmoothness");
    }

    public GenNoiseStack() {}

    public GenNoiseStack(GenNoiseConfig.NoiseType noiseType)
    {
        SetDefaults(noiseType);
    }

    public ModuleBase BuildModule(WorldTileInfo tile, Map map, string name, QualityMode qualityMode)
    {
        int seed = PerlinSeed == 0 ? Rand.Range(0, int.MaxValue) : PerlinSeed;
        ModuleBase module = new Perlin(PerlinFrequency, PerlinLacunarity, 
            PerlinPersistence, PerlinOctaves, seed, qualityMode);

        ModuleBase moduleMultiply = BuildModule(tile, map, CombineMethod.Multiply);
        if (moduleMultiply != null) module = new Multiply(module, moduleMultiply);
        
        module = new ScaleBias(BaseScale, BaseBias, module);
        NoiseDebugUI.StoreNoiseRender(module, name + " base", new IntVec2(map.Size.x, map.Size.z));
        
        ModuleBase moduleAdd = BuildModule(tile, map, CombineMethod.Add);
        if (moduleAdd != null) module = new Add(module, moduleAdd);
        
        ModuleBase moduleMin = BuildModule(tile, map, CombineMethod.Min);
        if (moduleMin != null) module = new SmoothMin(module, moduleMin, MinSmoothness);
        
        ModuleBase moduleMax = BuildModule(tile, map, CombineMethod.Max);
        if (moduleMax != null) module = new SmoothMax(module, moduleMax, MaxSmoothness);

        NoiseDebugUI.StoreNoiseRender(module, name + " combined", new IntVec2(map.Size.x, map.Size.z));
        return module;
    }

    public ModuleBase BuildModule(WorldTileInfo tile, Map map, CombineMethod forApplyMethod)
    {
        ModuleBase combinedModule = null;

        if (Entries.TryGetValue(forApplyMethod, out Entry entry))
        {
            foreach (GenNoiseLayer layer in entry.Layers)
            {
                float? fixedRotation = null;
                if (layer.AlignWithRiver && tile.River != null)
                {
                    fixedRotation = - tile.RiverAngle % 180f;
                } 
                else if (layer.AlignWithMainRoad && tile.MainRoad != null)
                {
                    fixedRotation = - tile.MainRoadAngle % 180f;
                }
                
                ModuleBase layerModule = new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
                    .Where(side => MatchesSide(side, fixedRotation != null ? Rot4.North : tile.LandformDirection, layer.MapSides))
                    .Where(_ => layer.ApplyChance >= 1f || Rand.Chance(layer.ApplyChance))
                    .Select(side => Rotate(layer.BuildModule(map), side, map))
                    .Aggregate<ModuleBase, ModuleBase>(null, (a, b) => Combine(layer.SideCombineMethod, a, b));
                
                if (layerModule == null) continue;

                float rotationOffset = layer.RotationOffset.RandomInRange;
                if (rotationOffset != 0f)
                    layerModule = Rotate(layerModule, rotationOffset, map);
                
                if (fixedRotation != null)
                    layerModule = Rotate(layerModule, fixedRotation.Value, map);

                if (layerModule != null)
                {
                    combinedModule = combinedModule == null
                        ? layerModule
                        : Combine(layer.LayerCombineMethod, combinedModule, layerModule);
                }
            }
        }

        return combinedModule;
    }

    private static bool MatchesSide(Rot4 side, Rot4 coastDirection, ISet<MapSide> mapSides)
    {
        return mapSides.Any(mapSide => mapSide switch
        {
            MapSide.Seaside => side == coastDirection,
            MapSide.Landside => side == coastDirection.Opposite,
            MapSide.Left => side == coastDirection.Rotated(RotationDirection.Counterclockwise),
            MapSide.Right => side == coastDirection.Rotated(RotationDirection.Clockwise),
            _ => throw new ArgumentOutOfRangeException(nameof(mapSide), mapSide, null)
        });
    }

    public enum MapSide
    {
        Seaside, Landside, Left, Right
    }

    public enum CombineMethod
    {
        Min, Max, Add, Multiply
    }

    public void DoSettingsWindowContents(Listing_Standard listingStandard, GenNoiseConfig.NoiseType pullDefaultsFrom)
    {
        Settings.CenteredLabel(listingStandard, "PerlinFrequency", Math.Round(PerlinFrequency, 2).ToString(CultureInfo.InvariantCulture));
        PerlinFrequency = listingStandard.Slider((float) PerlinFrequency, 0.01f, 0.1f);
        Settings.CenteredLabel(listingStandard, "PerlinLacunarity", Math.Round(PerlinLacunarity, 2).ToString(CultureInfo.InvariantCulture));
        PerlinLacunarity = listingStandard.Slider((float) PerlinLacunarity, 0f, 5f);
        Settings.CenteredLabel(listingStandard, "PerlinPersistence", Math.Round(PerlinPersistence, 2).ToString(CultureInfo.InvariantCulture));
        PerlinPersistence = listingStandard.Slider((float) PerlinPersistence, 0f, 1f);
        Settings.CenteredLabel(listingStandard, "PerlinOctaves", PerlinOctaves.ToString(CultureInfo.InvariantCulture));
        PerlinOctaves = (int) listingStandard.Slider(PerlinOctaves, 1f, 10f);
        
        listingStandard.Gap();
        int.TryParse(Settings.TextEntry(listingStandard, "NoiseSeed", PerlinSeed.ToString()), out PerlinSeed);
        
        listingStandard.Gap();
        Settings.CenteredLabel(listingStandard, "BaseScale", Math.Round(BaseScale, 2).ToString(CultureInfo.InvariantCulture));
        BaseScale = listingStandard.Slider((float) BaseScale, 0f, 1f);
        Settings.CenteredLabel(listingStandard, "BaseBias", Math.Round(BaseBias, 2).ToString(CultureInfo.InvariantCulture));
        BaseBias = listingStandard.Slider((float) BaseBias, 0f, 1f);

        listingStandard.Gap(18f);
        Settings.Dropdown(listingStandard, "Edit for ApplyMethod: ", Settings.SelectedApplyMethod, m => Settings.SelectedApplyMethod = m);

        if (Settings.SelectedApplyMethod == CombineMethod.Min)
        {
            listingStandard.Gap();
            Settings.CenteredLabel(listingStandard, "MinSmoothness", Math.Round(MinSmoothness, 2).ToString(CultureInfo.InvariantCulture));
            MinSmoothness = listingStandard.Slider((float) MinSmoothness, 0f, 10f);
        } 
        else if (Settings.SelectedApplyMethod == CombineMethod.Max)
        {
            listingStandard.Gap();
            Settings.CenteredLabel(listingStandard, "MaxSmoothness", Math.Round(MaxSmoothness, 2).ToString(CultureInfo.InvariantCulture));
            MaxSmoothness = listingStandard.Slider((float) MaxSmoothness, 0f, 10f);
        }

        listingStandard.Gap();
        Entries.TryGetValue(Settings.SelectedApplyMethod, out Entry entry);
        if (entry == null) Entries.Add(Settings.SelectedApplyMethod, entry = new Entry());
        int layerCount = entry.Layers.Count;
        listingStandard.Label("LayerCount: " + layerCount.ToString(CultureInfo.InvariantCulture));
        listingStandard.IntAdjuster(ref layerCount, 1);
        while (entry.Layers.Count > layerCount) entry.Layers.RemoveAt(entry.Layers.Count - 1);
        while (entry.Layers.Count < layerCount) entry.Layers.Add(new GenNoiseLayer(pullDefaultsFrom, Settings.SelectedApplyMethod));

        listingStandard.Gap();
        for (var i = 0; i < entry.Layers.Count; i++)
        {
            GenNoiseLayer layer = entry.Layers[i];
            
            listingStandard.Gap(36f);
            layer.DoSettingsWindowContents(listingStandard, i);
        }
    }

    public void SetDefaults(GenNoiseConfig.NoiseType noiseType)
    {
        if (noiseType == GenNoiseConfig.NoiseType.Coast)
        {
            PerlinFrequency = 0.03f;
            PerlinLacunarity = 2f;
            PerlinPersistence = 0.5f;
            PerlinOctaves = 3;
            BaseScale = 0.5f;
            BaseBias = 0.5f;
            return;
        }
        
        if (noiseType is GenNoiseConfig.NoiseType.Elevation or GenNoiseConfig.NoiseType.Fertility)
        {
            PerlinFrequency = 0.021f;
            PerlinLacunarity = 2f;
            PerlinPersistence = 0.5f;
            PerlinOctaves = 6;
            BaseScale = 0.5f;
            BaseBias = 0.5f;
        }
    }

    public static ModuleBase Combine(CombineMethod combineMethod, params ModuleBase[] modules)
    {
        return modules.Aggregate<ModuleBase, ModuleBase>(null, 
            (a, b) => a == null ? b : b == null ? a :
            combineMethod switch
            {
                CombineMethod.Min => new Min(a, b),
                CombineMethod.Max => new Max(a, b),
                CombineMethod.Add => new Add(a, b),
                CombineMethod.Multiply => new Multiply(a, b),
                _ => throw new ArgumentOutOfRangeException(nameof(combineMethod), combineMethod, null)
            });
    }
    
    public static ModuleBase Rotate(ModuleBase input, Rot4 rot4, Map map)
    {
        if (rot4 == Rot4.North)
            return Rotate(input, -90f, map);
        if (rot4 == Rot4.East)
            return Rotate(input, 180f, map);
        if (rot4 == Rot4.South)
            return Rotate(input, 90f, map);
        return input;
    }
    
    public static ModuleBase Rotate(ModuleBase input, float angle, Map map)
    {
        if (angle == 0f) return input;
        return new Translate(-map.Size.x * 0.5f, 0f, -map.Size.z * 0.5f, new Rotate(0f, angle, 0f, new Translate(map.Size.x * 0.5f, 0f, map.Size.z * 0.5f, input)));
    }
}
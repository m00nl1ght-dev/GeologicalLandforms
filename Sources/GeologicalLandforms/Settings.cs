using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class Settings : ModSettings
{
    public Dictionary<string, Landform> DefaultLandforms;
    public Dictionary<string, Landform> CustomLandforms;
    
    public bool UseCustomConfig;
    public bool CustomDataModified;
    
    public static Landform SelectedLandform;
    public static GenNoiseConfig.NoiseType SelectedNoiseType = GenNoiseConfig.NoiseType.Coast;
    public static GenNoiseStack.CombineMethod SelectedApplyMethod = GenNoiseStack.CombineMethod.Add;

    public Dictionary<string, Landform> Landforms => UseCustomConfig ? CustomLandforms : DefaultLandforms;

    private static Vector2 _scrollPos = Vector2.zero;

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect rect = new(0.0f, 0.0f, inRect.width, 100f + (UseCustomConfig ? 10000f : 0f));
        rect.xMax *= 0.95f;
        
        Listing_Standard listingStandard = new();
        listingStandard.Begin(rect);
        GUI.EndGroup();
        Widgets.BeginScrollView(inRect, ref _scrollPos, rect);

        listingStandard.CheckboxLabeled("GeologicalLandforms.Settings.Description".Translate(), ref UseCustomConfig);
        if (UseCustomConfig)
        {
            listingStandard.Gap(24f);
            Dropdown(listingStandard, "GeologicalLandforms.Settings.SelectLandform".Translate(), 
                SelectedLandform?.Id ?? "None", CustomLandforms.Keys.ToList(), e => SelectedLandform = CustomLandforms[e]);

            if (SelectedLandform != null)
            {
                listingStandard.Gap();
                SelectedLandform.DoSettingsWindowContents(listingStandard);
                CustomDataModified = true;
            
                listingStandard.Gap();
                if (listingStandard.ButtonText("GeologicalLandforms.Settings.Copy".Translate()))
                    Copy();
                
                if (!SelectedLandform.IsCustom && listingStandard.ButtonText("GeologicalLandforms.Settings.Reset".Translate()))
                    Reset();
                
                if (SelectedLandform is { IsCustom: true } && listingStandard.ButtonText("GeologicalLandforms.Settings.Delete".Translate()))
                    Delete();
            }

            listingStandard.Gap();
            if (listingStandard.ButtonText("GeologicalLandforms.Settings.ResetAll".Translate()))
                ResetAll();
        }

        Widgets.EndScrollView();
    }

    public static void FloatRangeSlider(Listing_Standard listingStandard, ref FloatRange floatRange, string name, float min, float max)
    {
        CenteredLabel(listingStandard, "", name);
        Rect rect = listingStandard.GetRect(28f);
        
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
            Widgets.FloatRange(rect, (int) listingStandard.CurHeight, ref floatRange, min, max);
            
        listingStandard.Gap(18f);
    }
    
    public static string TextEntry(Listing_Standard listingStandard, string name, string value, float labelWidth = 200f)
    {
        Rect rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), name);
            value = Widgets.TextField(rect.RightPartPixels(rect.width - labelWidth), value);
        }
        
        listingStandard.Gap();
        return value;
    }
    
    public static void Dropdown<T>(Listing_Standard listingStandard, string name, T value, List<T> potentialValues, Action<T> action, float labelWidth = 200f)
    {
        Rect rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), name);
            Rect right = rect.RightPartPixels(rect.width - labelWidth);
            if (Widgets.ButtonText(right, value.ToString()))
            {
                List<FloatMenuOption> options = potentialValues.Select(e => new FloatMenuOption(e.ToString(), () => action(e))).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        
        listingStandard.Gap();
    }
    
    public static void Dropdown<T>(Listing_Standard listingStandard, string name, T value, Action<T> action, float labelWidth = 200f) where T : Enum
    {
        Dropdown(listingStandard, name, value, typeof(T).GetEnumValues().Cast<T>().ToList(), action, labelWidth);
    }

    public static void CenteredLabel(Listing_Standard listingStandard, string left, string center)
    {
        Vector2 labelSize = Text.CalcSize(center);
        Rect rect = listingStandard.GetRect(28f);
        Rect centerRect = rect.RightHalf();
        centerRect.xMin -= labelSize.x * 0.5f;
        Widgets.Label(centerRect, center);
        Widgets.Label(rect, left);
    }

    public static void Checkboxes<T>(Listing_Standard listingStandard, string label, ref HashSet<T> values, float rectSize, float labelWidth) where T : Enum
    {
        Rect rect = listingStandard.GetRect(28f);
        
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), label);
            rect = rect.RightPartPixels(rect.width - labelWidth);
            foreach (T value in typeof(T).GetEnumValues().Cast<T>())
            {
                bool selected = values.Contains(value);
                Widgets.Checkbox(rect.min, ref selected);
                rect.xMin += 35f;
                Widgets.Label(rect, value.ToString());
                if (selected) values.Add(value); else values.Remove(value);
                rect.xMin += rectSize;
            }
        }
        
        listingStandard.Gap(listingStandard.verticalSpacing);
    }
    
    public static void Checkboxes(Listing_Standard listingStandard, string label, float rectSize, float labelWidth, ref bool[] values, params string[] labels)
    {
        Rect rect = listingStandard.GetRect(28f);
        
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), label);
            rect = rect.RightPartPixels(rect.width - labelWidth);
            for (var i = 0; i < values.Length; i++)
            {
                bool selected = values[i];
                Widgets.Checkbox(rect.min, ref selected);
                rect.xMin += 35f;
                Widgets.Label(rect, labels[i]);
                values[i] = selected;
                rect.xMin += rectSize;
            }
        }
        
        listingStandard.Gap(listingStandard.verticalSpacing);
    }
    
    public static void RadioButtons<T>(Listing_Standard listingStandard, string label, ref T currentValue, float rectSize, float labelWidth) where T : Enum
    {
        Rect rect = listingStandard.GetRect(28f);
        
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), label);
            rect = rect.RightPartPixels(rect.width - labelWidth);
            foreach (T value in typeof(T).GetEnumValues().Cast<T>())
            {
                if (Widgets.RadioButton(rect.xMin, rect.yMin, value.Equals(currentValue))) currentValue = value;
                rect.xMin += 35f;
                Widgets.Label(rect, value.ToString());
                rect.xMin += rectSize;
            }
        }
        
        listingStandard.Gap(listingStandard.verticalSpacing);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref UseCustomConfig, "UseCustomConfig");
        base.ExposeData();
    }
    
    public void Copy()
    {
        if (SelectedLandform == null) return;
        Landform landform = LoadLandformsFromDirectory(ModInstance.CustomLandformsDir, DefaultLandforms).TryGetValue(SelectedLandform.Id);
        if (landform == null) return;
        
        string newId = SelectedLandform.Id + "Copy";
        while (CustomLandforms.ContainsKey(newId) || DefaultLandforms.ContainsKey(newId))
        {
            newId += "Copy";
        }
        
        landform.IsCustom = true;
        landform.Id = newId;
        
        CustomLandforms.Add(newId, landform);
        SelectedLandform = landform;
    }
    
    public void Delete()
    {
        if (SelectedLandform == null) return;
        CustomLandforms.Remove(SelectedLandform.Id);
        SelectedLandform = null;
    }

    public void Reset()
    {
        if (SelectedLandform == null) return;
        Landform landform = LoadLandformsFromDirectory(ModInstance.DefaultLandformsDir, null).TryGetValue(SelectedLandform.Id);
        if (landform != null) CustomLandforms[SelectedLandform.Id] = landform;
        SelectedLandform = landform;
    }
    
    public void ResetAll()
    {
        CustomLandforms = LoadLandformsFromDirectory(ModInstance.DefaultLandformsDir, null);
        SelectedLandform = null;
        SelectedNoiseType = GenNoiseConfig.NoiseType.Coast;
        SelectedApplyMethod = GenNoiseStack.CombineMethod.Add;
        UseCustomConfig = false;
    }
    
    public static Dictionary<string, Landform> LoadLandformsFromDirectory(string directory, Dictionary<string, Landform> fallback)
    {
        Dictionary<string, Landform> landforms = new(fallback ?? new());
        
        foreach (string file in Directory.GetFiles(directory, "*.xml"))
        {
            Landform landform = null;
            
            try
            {
                if (File.Exists(file))
                {
                    Scribe.loader.InitLoading(file);
                    try
                    {
                        Scribe_Deep.Look(ref landform, "Landform");
                    }
                    finally
                    {
                        Scribe.loader.FinalizeLoading();
                    }
                }
                
                if (landform != null)
                {
                    if (landforms.ContainsKey(landform.Id)) landforms[landform.Id] = landform;
                    else landforms.Add(landform.Id, landform);
                    Log.Message($"Loaded landform {landform.Id} from file {file}.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Caught exception while loading landform from file {file}. The exception was: {ex}");
            }
        }

        return landforms;
    }
    
    public static void SaveLandformsToDirectory(string directory, Dictionary<string, Landform> landscapes)
    {
        foreach (string file in Directory.GetFiles(directory, "*.xml"))
        {
            File.Delete(file);
        }

        foreach (KeyValuePair<string, Landform> pair in landscapes)
        {
            string file = Path.Combine(directory, pair.Key + ".xml");
            Scribe.saver.InitSaving(file, "CustomLandformData");
            Landform landform = pair.Value;
            try
            {
                Scribe_Deep.Look(ref landform, "Landform");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
        }
    }
}
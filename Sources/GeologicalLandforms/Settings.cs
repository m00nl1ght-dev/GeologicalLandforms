using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using static GeologicalLandforms.Settings;

namespace GeologicalLandforms;

public class Settings : ModSettings
{
    public Dictionary<string, Landform> DefaultLandforms;
    public Dictionary<string, Landform> CustomLandforms;

    public int MaxLandformSearchRadius = 50;
    
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
        
        CenteredLabel(listingStandard, "GeologicalLandforms.Settings.MaxLandformSearchRadius".Translate(), MaxLandformSearchRadius.ToString(CultureInfo.InvariantCulture));
        MaxLandformSearchRadius = (int) listingStandard.Slider(MaxLandformSearchRadius, 10f, 500f);

        listingStandard.CheckboxLabeled("GeologicalLandforms.Settings.Description".Translate(), ref UseCustomConfig);
        if (UseCustomConfig)
        {
            listingStandard.Gap();
            if (listingStandard.ButtonText("GeologicalLandforms.Settings.ResetAll".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmResetAll".Translate(), ResetAll));
            }

            if (listingStandard.ButtonText("GeologicalLandforms.Settings.OpenDataDir".Translate()))
            {
                SaveLandformsToDirectory(ModInstance.CustomLandformsDir, Main.Settings.CustomLandforms);
                Application.OpenURL(ModInstance.CustomLandformsDir);
            }

            listingStandard.Gap(24f);
            Dropdown(listingStandard, "GeologicalLandforms.Settings.SelectLandform".Translate(), 
                SelectedLandform, CustomLandforms.Values.ToList(), e => SelectedLandform = e, 200f,
                e => e?.TranslatedName.CapitalizeFirst() ?? "GeologicalLandforms.Settings.SelectedNone".Translate());

            if (SelectedLandform != null)
            {
                listingStandard.Gap();
                if (listingStandard.ButtonText("GeologicalLandforms.Settings.Copy".Translate()))
                    Copy();

                if (!SelectedLandform.IsCustom && listingStandard.ButtonText("GeologicalLandforms.Settings.Reset".Translate()))
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmReset".Translate(), Reset));
                
                if (SelectedLandform is { IsCustom: true } && listingStandard.ButtonText("GeologicalLandforms.Settings.Delete".Translate()))
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmDelete".Translate(), Delete));
                
                listingStandard.Gap(24f);
                SelectedLandform.DoSettingsWindowContents(listingStandard);
                CustomDataModified = true;
            }
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
    
    public static void Dropdown<T>(Listing_Standard listingStandard, string name, T value, List<T> potentialValues, 
        Action<T> action, float labelWidth = 200f, Func<T, string> textFunc = null)
    {
        Rect rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), name);
            Rect right = rect.RightPartPixels(rect.width - labelWidth);
            if (Widgets.ButtonText(right, textFunc != null ? textFunc.Invoke(value) : value.ToString()))
            {
                List<FloatMenuOption> options = potentialValues.Select(e => 
                    new FloatMenuOption(textFunc != null ? textFunc.Invoke(e) : e.ToString(), () => action(e))).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        
        listingStandard.Gap();
    }
    
    public static void Dropdown<T>(Listing_Standard listingStandard, string name, T value, Action<T> action, float labelWidth = 200f, string translationKeyPrefix = null) where T : Enum
    {
        Dropdown(listingStandard, name, value, typeof(T).GetEnumValues().Cast<T>().ToList(), action, labelWidth, 
            translationKeyPrefix == null ? null : e => (translationKeyPrefix + "." + e).Translate());
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

    [SuppressMessage("ReSharper", "RedundantCast")]
    public static void Checkboxes<T>(Listing_Standard listingStandard, string label, ref HashSet<T> values, 
        float rectSize, float labelWidth, string translationKeyPrefix = null) where T : Enum
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
                Widgets.Label(rect, translationKeyPrefix == null ? value.ToString() : (string)(translationKeyPrefix + "." + value).Translate());
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
    
    [SuppressMessage("ReSharper", "RedundantCast")]
    public static void RadioButtons<T>(Listing_Standard listingStandard, string label, ref T currentValue, 
        float rectSize, float labelWidth, string translationKeyPrefix = null) where T : Enum
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
                Widgets.Label(rect, translationKeyPrefix == null ? value.ToString() : (string)(translationKeyPrefix + "." + value).Translate());
                rect.xMin += rectSize;
            }
        }
        
        listingStandard.Gap(listingStandard.verticalSpacing);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref UseCustomConfig, "UseCustomConfig");
        Scribe_Values.Look(ref MaxLandformSearchRadius, "MaxLandformSearchRadius", 50);
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
        MaxLandformSearchRadius = 50;
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
                    // Log.Message($"Loaded landform {landform.Id} from file {file}.");
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
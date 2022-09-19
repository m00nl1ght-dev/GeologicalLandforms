using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public static class GuiUtils
{
    public static void FloatRangeSlider(Listing_Standard listingStandard, ref FloatRange floatRange, string name, float min, float max)
    {
        CenteredLabel(listingStandard, "", name);
        Rect rect = listingStandard.GetRect(28f);
        FloatRange before = floatRange;
        
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
            Widgets.FloatRange(rect, (int) listingStandard.CurHeight, ref floatRange, min, max);

        if (floatRange != before) GUI.changed = true;
            
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
    
    public static void IntEntry(Listing_Standard listingStandard, string name, ref int value, ref string editBuffer, int min, int max, float labelWidth = 200f)
    {
        Rect rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            Widgets.Label(rect.LeftPartPixels(labelWidth), name);
            Widgets.TextFieldNumeric(rect.RightPartPixels(rect.width - labelWidth), ref value, ref editBuffer, min, max);
        }
        
        listingStandard.Gap();
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
                    new FloatMenuOption(textFunc != null ? textFunc.Invoke(e) : e.ToString(), () => { action(e); GUI.changed = true; })).ToList();
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

    public static void Tooltip(string tooltip)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            TooltipHandler.TipRegion(lastRect, new TipSignal(tooltip));
        }
    }
}
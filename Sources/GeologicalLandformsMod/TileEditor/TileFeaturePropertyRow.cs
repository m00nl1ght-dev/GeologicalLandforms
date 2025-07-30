using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using LunarFramework.GUI;
using MapPreview;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TileEditor;

[HotSwappable]
public abstract class TileFeaturePropertyRow
{
    public static List<TileFeaturePropertyRow> SetupRowsForFeature(TileMutatorDef feature)
    {
        if (feature == null) return [];

        var rows = new List<TileFeaturePropertyRow>();

        if (feature.AsLandform() is {} landform)
        {
            foreach (var config in landform.ConfigurableOverrides.OrderBy(c => c.SortOrder))
            {
                if (config.KnobName == "Apply chance" && config.KnobType == ValueFunctionConnection.Id)
                {
                    rows.Add(new LandformOverrideApplyChance(landform, config));
                }
                else
                {
                    LandformOverride row = config.KnobType switch
                    {
                        ValueFunctionConnection.Id => new LandformOverrideNumber(landform, config),
                        TerrainFunctionConnection.Id => new LandformOverrideDef<TerrainDef>(landform, config),
                        BiomeFunctionConnection.Id => new LandformOverrideDef<BiomeDef>(landform, config),
                        RoofFunctionConnection.Id => new LandformOverrideDef<RoofDef>(landform, config),
                        _ => null
                    };

                    if (row != null)
                        rows.Add(row);
                }
            }
        }

        return rows;
    }

    public abstract void DoEditorUI(LayoutRect layout, TileEditorData data, Action onChange);

    public abstract class LandformOverride : TileFeaturePropertyRow
    {
        public readonly Landform Landform;
        public readonly ConfigurableOverride Config;

        protected LandformOverride(Landform landform, ConfigurableOverride config)
        {
            Landform = landform;
            Config = config;
        }
    }

    public class LandformOverrideNumber : LandformOverride
    {
        public LandformOverrideNumber(Landform landform, ConfigurableOverride config) : base(landform, config) {}

        private string _editBuffer;
        private double? _lastNaturalValue;

        public override void DoEditorUI(LayoutRect layout, TileEditorData data, Action onChange)
        {
            if (Config.LastNaturalValue == null && !MapPreviewAPI.IsGeneratingPreview) return;

            _lastNaturalValue = (double?) Config.LastNaturalValue ?? _lastNaturalValue;

            if (_lastNaturalValue == null) return;

            using (layout.Row(24f))
            {
                var rowRect = (Rect) layout;
                rowRect.width -= 5f;

                Widgets.DrawBoxSolid(rowRect, TileEditorWindow.ColorSectionHeader);

                var label = Config.PropertyLabel.NullOrEmpty() ? "Nameless Property" : Config.PropertyLabel;
                LunarGUI.Label(layout.Abs(160f).Moved(5f, 2f), label);

                using (layout.RowRev())
                {
                    var valueBefore = Math.Round(_lastNaturalValue.Value, 4);

                    if (data.TryGetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, out var value))
                        valueBefore = double.Parse(value, CultureInfo.InvariantCulture);

                    var valueAfter = valueBefore;

                    LunarGUI.DoubleField(layout.Abs(70f), ref valueAfter, ref _editBuffer, Config.MinValue, Config.MaxValue);

                    LunarGUI.Slider(layout.Abs(-1).Moved(-3f, 7f), ref valueAfter, Config.MinValue, Config.MaxValue, 4);

                    if (valueAfter != valueBefore)
                    {
                        data.SetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, valueAfter.ToString(CultureInfo.InvariantCulture));
                        onChange();
                    }
                }
            }
        }
    }

    public class LandformOverrideDef<T> : LandformOverride where T : Def
    {
        public LandformOverrideDef(Landform landform, ConfigurableOverride config) : base(landform, config) {}

        private T _lastNaturalValue;

        public override void DoEditorUI(LayoutRect layout, TileEditorData data, Action onChange)
        {
            if (Config.LastNaturalValue == null && !MapPreviewAPI.IsGeneratingPreview) return;

            _lastNaturalValue = (T) Config.LastNaturalValue ?? _lastNaturalValue;

            if (_lastNaturalValue == null) return;

            using (layout.Row(24f))
            {
                Widgets.DrawBoxSolid(layout, TileEditorWindow.ColorSectionHeader);

                var label = Config.PropertyLabel.NullOrEmpty() ? "Nameless Property" : Config.PropertyLabel;
                LunarGUI.Label(layout.Abs(160f).Moved(5f, 2f), label);

                using (layout.RowRev())
                {
                    var currentValue = _lastNaturalValue;

                    if (data.TryGetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, out var value))
                        currentValue = DefDatabase<T>.GetNamedSilentFail(value);

                    if (TileEditorWindow.DoIconButton(layout, TileEditorWindow.IconEditElement, Color.white, 2f))
                    {
                        Find.WindowStack.Add(new DefSelectionWindow<T>(SetValue, TileEditorWindow.ProblemType.Info));

                        void SetValue(T value)
                        {
                            data.SetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, value.defName);
                            onChange();
                        }
                    }

                    LunarGUI.Label(layout.Abs(-1).Moved(5f, 2f), currentValue?.LabelCap ?? "None");
                }
            }
        }
    }

    [HotSwappable]
    public class LandformOverrideApplyChance : LandformOverride
    {
        public LandformOverrideApplyChance(Landform landform, ConfigurableOverride config) : base(landform, config) {}

        public override void DoEditorUI(LayoutRect layout, TileEditorData data, Action onChange)
        {
            using (layout.Row(24f))
            {
                Widgets.DrawBoxSolid(layout, TileEditorWindow.ColorSectionHeader);

                var label = Config.PropertyLabel.NullOrEmpty() ? "Nameless Property" : Config.PropertyLabel;
                LunarGUI.Label(layout.Abs(160f).Moved(5f, 2f), label);

                using (layout.RowRev())
                {
                    var valueBefore = MultiCheckboxState.Partial;

                    if (data.TryGetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, out var value))
                        valueBefore = value == "0" ? MultiCheckboxState.Off : MultiCheckboxState.On;

                    var valueAfter = LunarGUI.CheckboxThreeStates(layout.Abs(24f), valueBefore);

                    if (valueAfter != valueBefore)
                    {
                        if (valueAfter != MultiCheckboxState.Partial)
                        {
                            var valueStr = valueAfter == MultiCheckboxState.On ? "1" : "0";
                            data.SetFeatureProperty(Landform.TileMutatorDef, Config.PropertyId, valueStr);
                        }
                        else
                        {
                            data.ClearFeatureProperty(Landform.TileMutatorDef, Config.PropertyId);
                        }

                        onChange();
                    }
                }
            }
        }
    }
}

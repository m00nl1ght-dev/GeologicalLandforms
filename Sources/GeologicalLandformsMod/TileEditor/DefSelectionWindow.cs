using System;
using System.Collections.Generic;
using System.Linq;
using LunarFramework.GUI;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TileEditor;

public class DefSelectionWindow<T> : Window where T : Def
{
    public override Vector2 InitialSize => new(500, 400);

    private readonly Action<T> _action;
    private readonly Func<T, Entry> _entryFunc;

    private readonly LayoutRect LayoutWindow = new(GeologicalLandformsMod.LunarAPI);
    private readonly LayoutRectScrollable LayoutListing = new(GeologicalLandformsMod.LunarAPI);

    private List<Entry> _entries = [];
    private string _searchString = "";

    public DefSelectionWindow(Action<T> action, Func<T, Entry> entryFunc)
    {
        _action = action;
        _entryFunc = entryFunc;
        absorbInputAroundWindow = true;
        layer = WindowLayer.SubSuper;
        RefreshEntries();
    }

    public override void DoWindowContents(Rect rect)
    {
        using (LayoutWindow.Root(rect))
        {
            LayoutWindow.PushChanged();

            LunarGUI.TextField(LayoutWindow.Abs(28f), ref _searchString);

            if (LayoutWindow.PopChanged())
            {
                _searchString = _searchString.ToLower();
                RefreshEntries();
            }

            LayoutWindow.Abs(Margin / 2);

            using (LayoutListing.RootScrollable(LayoutWindow.Rel(-1), new() { Spacing = 5f }))
            {
                foreach (var entry in _entries)
                {
                    using (LayoutListing.Row(24f))
                    {
                        var color = Mouse.IsOver(LayoutListing) ? TileEditorWindow.ColorSectionBackground : TileEditorWindow.ColorSectionHeader;

                        Widgets.DrawBoxSolid(LayoutListing, color);

                        LunarGUI.Label(LayoutListing.Rel(0.5f).Moved(5f, 2f), entry.Label);

                        using (LayoutListing.RowRev())
                        {
                            foreach (var problem in entry.ProblemMessages)
                            {
                                var iconRect = LayoutListing.Abs(24f);
                                var iconTex = entry.PreventSelection ? TileEditorWindow.IconWarningRed : TileEditorWindow.IconWarningYellow;

                                GUI.DrawTexture(iconRect.ContractedBy(2f), iconTex);

                                TooltipHandler.TipRegion(iconRect, problem);
                            }

                            if (entry.SubLabel != null)
                            {
                                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                                LunarGUI.Label(LayoutListing.Abs(-1).Moved(5f, 2f), entry.SubLabel);
                                GUI.color = Color.white;
                            }
                        }

                        if (!entry.PreventSelection && Widgets.ButtonInvisible(LayoutListing))
                        {
                            Close();
                            _action(entry.Value);
                        }
                    }
                }
            }
        }
    }

    private void RefreshEntries()
    {
        var entries = DefDatabase<T>.AllDefs.Select(_entryFunc).Where(e => e != null);

        if (_searchString.Length > 0)
        {
            entries = entries.Where(d =>
                d.Label?.ToLower().Contains(_searchString) == true ||
                d.SubLabel?.ToLower().Contains(_searchString) == true
            );
        }

        entries = entries.OrderBy(d => TileEditorWindow.DefMcpSort(d.Value.modContentPack))
            .ThenBy(d => d.Value.modContentPack?.Name)
            .ThenBy(d => d.Label);

        _entries = entries.ToList();

        LayoutListing.ResetScroll();
    }

    public class Entry
    {
        public T Value;
        public string Label;
        public string SubLabel;
        public List<string> ProblemMessages = [];
        public bool PreventSelection;

        public Entry(T value)
        {
            Value = value;
            Label = value.LabelCap;
            SubLabel = value.modContentPack?.Name ?? "Unknown Source";
        }
    }
}

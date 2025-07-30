using System;
using System.Collections.Generic;
using System.Linq;
using LunarFramework.GUI;
using UnityEngine;
using Verse;
using static GeologicalLandforms.TileEditor.TileEditorWindow;

namespace GeologicalLandforms.TileEditor;

public class DefSelectionWindow<T> : Window where T : Def
{
    public override Vector2 InitialSize => new(500, 400);

    public static Func<T, Entry> DefaultEntryFactory = v => new Entry(v);

    private readonly Action<T> _action;
    private readonly Func<T, Entry> _entryFactory;

    private readonly LayoutRect LayoutWindow = new(GeologicalLandformsMod.LunarAPI);
    private readonly LayoutRectScrollable LayoutListing = new(GeologicalLandformsMod.LunarAPI);

    private List<Entry> _entries = [];
    private string _searchString = "";
    private ProblemType _problemFilter;

    public DefSelectionWindow(Action<T> action, ProblemType initialFilter, Func<T, Entry> entryFactory = null)
    {
        _action = action;
        _entryFactory = entryFactory ?? DefaultEntryFactory;
        _problemFilter = initialFilter;
        absorbInputAroundWindow = true;
        layer = WindowLayer.SubSuper;
        RefreshEntries();
    }

    public override void DoWindowContents(Rect rect)
    {
        using (LayoutWindow.Root(rect))
        {
            using (LayoutWindow.RowRev(24f))
            {
                var filterIconRect = LayoutWindow.Abs(24f).Moved(4f, 0f);

                GUI.color = _problemFilter switch {
                    ProblemType.Warning => Color.yellow,
                    ProblemType.Error => Color.red,
                    _ => Color.white
                };

                GUI.DrawTexture(filterIconRect, IconFilter);

                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(filterIconRect))
                {
                    _problemFilter = _problemFilter switch {
                        ProblemType.Warning => ProblemType.Error,
                        ProblemType.Error => ProblemType.Info,
                        _ => ProblemType.Warning
                    };

                    RefreshEntries();
                }

                LayoutWindow.PushChanged();

                GUI.SetNextControlName("DefWindowSearchField");

                LunarGUI.TextField(LayoutWindow.Abs(-1), ref _searchString);

                GUI.FocusControl("DefWindowSearchField");

                if (LayoutWindow.PopChanged())
                {
                    _searchString = _searchString.ToLower();
                    RefreshEntries();
                }
            }

            LayoutWindow.Abs(Margin / 2);

            using (LayoutListing.RootScrollable(LayoutWindow.Rel(-1), new() { Spacing = 5f }))
            {
                foreach (var entry in _entries)
                {
                    using (LayoutListing.Row(24f))
                    {
                        var color = Mouse.IsOver(LayoutListing) ? ColorSectionBackground : ColorSectionHeader;

                        Widgets.DrawBoxSolid(LayoutListing, color);

                        LunarGUI.Label(LayoutListing.Rel(0.5f).Moved(5f, 2f), entry.Label);

                        using (LayoutListing.RowRev())
                        {
                            foreach (var problem in entry.Problems)
                            {
                                var iconRect = LayoutListing.Abs(24f);
                                GUI.DrawTexture(iconRect.ContractedBy(2f), problem.Icon);
                                TooltipHandler.TipRegion(iconRect, problem.message);
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
        var entries = DefDatabase<T>.AllDefs.Select(_entryFactory).Where(e => e != null);

        if (_searchString.Length > 0)
        {
            entries = entries.Where(d =>
                d.Label?.ToLower().Contains(_searchString) == true ||
                d.SubLabel?.ToLower().Contains(_searchString) == true
            );
        }

        if (_problemFilter != ProblemType.Error)
        {
            entries = entries.Where(e => e.Problems.All(p => p.type <= _problemFilter));
        }

        entries = entries.OrderBy(d => DefMcpSort(d.Value.modContentPack))
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
        public List<Problem> Problems = [];

        public bool PreventSelection => Problems.Any(p => p.type == ProblemType.Error);

        public Entry(T value)
        {
            Value = value;
            Label = value.LabelCap;
            SubLabel = value.modContentPack?.Name ?? "Unknown Source";
        }

        public void AddInfo(string message)
        {
            Problems.Add(new Problem(ProblemType.Info, message));
        }

        public void AddWarning(string message)
        {
            Problems.Add(new Problem(ProblemType.Warning, message));
        }

        public void AddError(string message)
        {
            Problems.Add(new Problem(ProblemType.Error, message));
        }
    }

    public readonly struct Problem
    {
        public readonly ProblemType type;
        public readonly string message;

        public Texture2D Icon => type switch
        {
            ProblemType.Info => TexButton.Info,
            ProblemType.Warning => IconWarningYellow,
            ProblemType.Error => IconWarningRed,
            _ => throw new ArgumentOutOfRangeException()
        };

        public Problem(ProblemType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }
}

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
    private readonly Predicate<T> _filter;

    private readonly LayoutRect LayoutWindow = new(GeologicalLandformsMod.LunarAPI);
    private readonly LayoutRectScrollable LayoutListing = new(GeologicalLandformsMod.LunarAPI);

    private List<T> _defsCached = [];
    private string _searchString = "";

    public DefSelectionWindow(Action<T> action, Predicate<T> filter)
    {
        _action = action;
        _filter = filter ?? (_ => true);
        absorbInputAroundWindow = true;
        layer = WindowLayer.SubSuper;
        RefreshDefList();
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
                RefreshDefList();
            }

            LayoutWindow.Abs(Margin / 2);

            using (LayoutListing.RootScrollable(LayoutWindow.Rel(-1), new() { Spacing = 5f }))
            {
                foreach (var def in _defsCached)
                {
                    using (LayoutListing.Row(24f))
                    {
                        var color = Mouse.IsOver(LayoutListing) ? TileEditorWindow.ColorSectionBackground : TileEditorWindow.ColorSectionHeader;

                        Widgets.DrawBoxSolid(LayoutListing, color);

                        LunarGUI.Label(LayoutListing.Rel(0.5f).Moved(5f, 2f), def.LabelCap);

                        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        LunarGUI.Label(LayoutListing.Abs(-1).Moved(5f, 2f), def.modContentPack.Name);
                        GUI.color = Color.white;

                        if (Widgets.ButtonInvisible(LayoutListing))
                        {
                            Close();
                            _action(def);
                        }
                    }
                }
            }
        }
    }

    private void RefreshDefList()
    {
        var defs = DefDatabase<T>.AllDefs;

        defs = defs.Where(d => !d.label.NullOrEmpty() && d.modContentPack != null && _filter(d));

        if (_searchString.Length > 0)
            defs = defs.Where(d => d.label.ToLower().Contains(_searchString) || d.modContentPack.Name.ToLower().Contains(_searchString));

        defs = defs.OrderBy(d => DefMcpSort(d.modContentPack))
            .ThenBy(d => d.modContentPack.Name)
            .ThenBy(d => d.label);

        _defsCached = defs.ToList();

        LayoutListing.ResetScroll();
    }

    private static int DefMcpSort(ModContentPack mcp)
    {
        return mcp.IsCoreMod ? 0 : mcp.IsOfficialMod ? 1 : 2;
    }
}

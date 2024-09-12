using LunarFramework.GUI;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeUIBase : NodeBase
{
    public Landform Landform => (Landform) canvas;

    public override bool AutoLayout => false;

    protected virtual float Margin => 15f;

    private readonly LayoutRect _layout = new(GeologicalLandformsAPI.LunarAPI);

    public override void NodeGUI()
    {
        var inner = GUILayoutUtility.GetRect(rect.width, rect.height);

        _layout.PushChanged();

        _layout.BeginRoot(inner, new LayoutParams { Margin = new(Margin) });
        DoWindowContents(_layout);
        _layout.End();

        if (_layout.PopChanged()) TerrainCanvas.OnNodeChange(this);
    }

    protected abstract void DoWindowContents(LayoutRect layout);

    protected static void DoCheckbox(Rect rect, ref bool value, string label)
    {
        var before = value;
        LunarGUI.HighlightOnHover(rect);
        var preAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        var labelRect = rect;
        labelRect.xMax -= 24f;
        Widgets.Label(labelRect, label);
        if (GUI.enabled && Widgets.ButtonInvisible(rect)) value = !value;
        Widgets.CheckboxDraw(rect.x + rect.width - 24, rect.y + (rect.height - 24) / 2f, value, !GUI.enabled);
        Text.Anchor = preAnchor;
        if (value != before) GUI.changed = true;
    }

    public override bool Calculate()
    {
        return true;
    }
}

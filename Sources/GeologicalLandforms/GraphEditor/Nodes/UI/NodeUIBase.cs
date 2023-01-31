using LunarFramework.GUI;
using TerrainGraph;
using UnityEngine;

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

    public override bool Calculate()
    {
        return true;
    }
}

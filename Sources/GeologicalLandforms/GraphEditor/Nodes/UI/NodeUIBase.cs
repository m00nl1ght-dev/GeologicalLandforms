using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public abstract class NodeUIBase : NodeBase
{
    public Landform Landform => (Landform) canvas;
    
    public override bool AutoLayout => false;
    
    protected virtual float Padding => 15f;
    
    public override void NodeGUI()
    {
        GUILayoutUtility.GetRect(rect.width, rect.height);
        Listing_Standard listingStandard = new();
        listingStandard.Begin(new Rect(Padding, Padding, rect.width - Padding * 2f, rect.height - Padding * 2f));
        DoWindowContents(listingStandard);
        listingStandard.End();
    }

    protected abstract void DoWindowContents(Listing_Standard listingStandard);

    public override bool Calculate()
    {
        return true;
    }
}
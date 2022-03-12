using System;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

[Serializable]
[Node(false, "Landform Manifest")]
public class NodeUILandformManifest : NodeUIBase
{
    public const string ID = "landformManifest";
    public override string GetID => ID;

    public override string Title => "Custom Landform";
    public override Vector2 DefaultSize => new(400, 150);
    
    public string Id;
    public bool IsCustom;
    
    public string DisplayName;
    public bool DisplayNameHasDirection;

    protected override void DoWindowContents(Listing_Standard listing)
    {
        GUI.enabled = Landform.IsCustom;
        
        string newId = GuiUtils.TextEntry(listing, "GeologicalLandforms.Settings.Landform.Id".Translate(), Id ?? "", 150f);
        DisplayName = GuiUtils.TextEntry(listing, "GeologicalLandforms.Settings.Landform.DisplayName".Translate(), DisplayName, 150f);
        listing.CheckboxLabeled("GeologicalLandforms.Settings.Landform.DisplayNameHasDirection".Translate(), ref DisplayNameHasDirection);
        listing.Gap();

        if (Id != newId)
        {
            LandformManager.Rename(Landform, newId);
        }

        GUI.enabled = true;
    }

    protected internal override void DrawNode()
    {
        if (IsCustom) base.DrawNode();
    }

    protected override void OnCreate()
    {
        if (Landform.Manifest != null && Landform.Manifest != this && canvas.nodes.Contains(Landform.Manifest)) Landform.Manifest.Delete();
        Landform.Manifest = this;
    }
}
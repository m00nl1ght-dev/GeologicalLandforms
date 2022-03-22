using System;
using System.IO;
using System.Linq;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Landform Manifest", 0)]
public class NodeUILandformManifest : NodeUIBase
{
    public const string ID = "landformManifest";
    public override string GetID => ID;

    public override string Title => "Custom Landform";
    public override Vector2 DefaultSize => new(400, 150);
    
    public string Id;
    public bool IsCustom = true;
    
    public int RevisionVersion = 1;
    public bool IsEdited;
    
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
            char[] invalids = Path.GetInvalidFileNameChars();
            newId = invalids.Aggregate(newId, (current, c) => current.Replace(c, '_'));
            LandformManager.Rename(Landform, newId);
        }

        GUI.enabled = true;
    }

    public override void DrawNode()
    {
        if (Landform.Id != null && IsCustom) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        NodeUILandformManifest existing = Landform.Manifest;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.Manifest = this;
    }
}
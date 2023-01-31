using System;
using System.IO;
using System.Linq;
using LunarFramework.GUI;
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
    public bool IsExperimental;

    public int RevisionVersion = 1;
    public long TimeCreated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public bool IsEdited;

    public string DisplayName;
    public bool DisplayNameHasDirection;

    protected override void DoWindowContents(LayoutRect layout)
    {
        layout.PushEnabled(Landform.IsCustom);

        var id = Id ?? "";

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.5f), "GeologicalLandforms.Settings.Landform.Id".Translate());
        LunarGUI.TextField(layout, ref id);
        layout.End();

        layout.Abs(10f);

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.5f), "GeologicalLandforms.Settings.Landform.DisplayName".Translate());
        LunarGUI.TextField(layout, ref DisplayName);
        layout.End();

        layout.Abs(10f);

        LunarGUI.Checkbox(layout, ref DisplayNameHasDirection, "GeologicalLandforms.Settings.Landform.DisplayNameHasDirection".Translate());

        if (id != Id)
        {
            char[] invalids = Path.GetInvalidFileNameChars();
            id = invalids.Aggregate(id, (current, c) => current.Replace(c, '_'));
            LandformManager.Rename(Landform, id);
        }

        layout.PopEnabled();
    }

    public override void DrawNode()
    {
        if (Landform.Id != null && IsCustom) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.Manifest;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.Manifest = this;
    }
}

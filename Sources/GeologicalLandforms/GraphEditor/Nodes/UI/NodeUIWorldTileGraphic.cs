using System;
using LunarFramework.GUI;
using NodeEditorFramework;
using UnityEngine;
using Verse;
using static GeologicalLandforms.WorldTileGraphicAtlas;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/Tile Graphic", 1001)]
public class NodeUIWorldTileGraphic : NodeUIBase
{
    public const string ID = "worldTileGraphic";
    public override string GetID => ID;

    public override string Title => "World Tile Graphic";
    public override Vector2 DefaultSize => new(400, 115);

    public readonly WorldTileGraphicAtlas Atlas = new();

    public DrawMode DrawMode = DrawMode.HexRandom;

    public int AtlasSizeX = 2;
    public int AtlasSizeY = 2;

    private string _atlasSizeXEdit;
    private string _atlasSizeYEdit;

    protected override void DoWindowContents(LayoutRect layout)
    {
        layout.PushChanged();

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.3f), "GeologicalLandforms.Settings.Landform.WorldTileGraphic.DrawMode".Translate());
        LunarGUI.Dropdown(layout, DrawMode, e => DrawMode = e, "GeologicalLandforms.Settings.Landform.WorldTileGraphic.DrawMode");
        layout.End();

        layout.Abs(10f);

        layout.BeginAbs(28f);
        LunarGUI.Label(layout.Rel(0.3f), "GeologicalLandforms.Settings.Landform.WorldTileGraphic.AtlasSize".Translate());
        LunarGUI.IntField(layout.Rel(0.35f), ref AtlasSizeX, ref _atlasSizeXEdit, 1, 64);
        LunarGUI.IntField(layout.Abs(-1), ref AtlasSizeY, ref _atlasSizeYEdit, 1, 64);
        layout.End();

        if (layout.PopChanged())
        {
            UpdateAtlas();
        }
    }

    private void UpdateAtlas()
    {
        Atlas.texture = "World/Landforms/" + Landform.Id;
        Atlas.atlasSize = new IntVec2(AtlasSizeX, AtlasSizeY);
        Atlas.drawMode = DrawMode;
        Atlas.Refresh();
    }

    public override void DrawNode()
    {
        if (Landform.Id != null) base.DrawNode();
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.WorldTileGraphic;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.WorldTileGraphic = this;

        LandformManager.AnyHasTileGraphic = true;
        UpdateAtlas();
    }

    protected override void OnDelete()
    {
        if (Landform.WorldTileGraphic == this) Landform.WorldTileGraphic = null;
    }
}

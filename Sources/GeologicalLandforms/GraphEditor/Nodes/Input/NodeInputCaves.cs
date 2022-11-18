using System;
using NodeEditorFramework;
using TerrainGraph;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Input/Caves", 354)]
public class NodeInputCaves : NodeInputBase
{
    public const string ID = "inputCaves";
    public override string GetID => ID;

    public override string Title => "Caves Input";

    public override ValueConnectionKnob KnobRef => Knob;

    [ValueConnectionKnob("Caves", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob Knob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.InputCaves;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.InputCaves = this;
    }

    protected override void OnDelete()
    {
        if (Landform.InputCaves == this) Landform.InputCaves = null;
    }

    public override bool Calculate()
    {
        var func = Landform.GetFeature(l => l.OutputCaves?.Get());

        if (func == null)
        {
            if (Landform.GeneratingTile is WorldTileInfo tile)
            {
                if (!tile.World.HasCaves(tile.TileId))
                {
                    Knob.SetValue(GridFunction.Zero);
                    return true;
                }
            }
            
            func = GridFunction.Zero; // TODO vanilla gen
        }
        
        Knob.SetValue(func);
        return true;
    }
}
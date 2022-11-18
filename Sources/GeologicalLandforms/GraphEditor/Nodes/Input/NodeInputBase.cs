using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
public abstract class NodeInputBase : NodeBase
{
    public Landform Landform => (Landform) canvas;
    
    public virtual ValueConnectionKnob KnobRef => null;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);
        
        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label(KnobRef.name, BoxLayout);
        GUILayout.EndHorizontal();
        KnobRef.SetPosition();

        GUILayout.EndVertical();
    }

    protected int TryGetVanillaGenStepSeed(int seedPart)
    {
        if (Landform.GeneratingTile is WorldTileInfo tile)
        {
            return Gen.HashCombineInt(Gen.HashCombineInt(tile.World.info.Seed, tile.TileId), seedPart);
        }

        return Gen.HashCombineInt(CombinedSeed, seedPart);
    }

    protected int TryGetVanillaGenStepRandValue(int seedPart, int iteration)
    {
        Rand.PushState();
        Rand.Seed = TryGetVanillaGenStepSeed(seedPart);
        var val = 0; for (int i = 0; i <= iteration; i++) val = Rand.Int;
        Rand.PopState();
        return val;
    }
}
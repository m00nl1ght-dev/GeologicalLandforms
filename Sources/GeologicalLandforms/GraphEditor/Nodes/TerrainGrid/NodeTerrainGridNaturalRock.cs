using System;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Terrain/Grid/Natural Rock", 315)]
public class NodeTerrainGridNaturalRock : NodeBase
{
    public const string ID = "terrainGridNaturalRock";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Natural Rock";

    [ValueConnectionKnob("Output", Direction.Out, TerrainGridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        GUILayout.BeginHorizontal(BoxStyle);
        GUILayout.Label("Rock Grid", DoubleBoxLayout);
        GUILayout.EndHorizontal();
        OutputKnob.SetPosition();

        GUILayout.EndVertical();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<TerrainDef>>>(Supplier.Of<IGridFunction<TerrainDef>>(new RockGridFunction()));
        return true;
    }

    private class RockGridFunction : IGridFunction<TerrainDef>
    {
        public TerrainDef ValueAt(double x, double z)
        {
            if (RockNoises.rockNoises == null) return ThingDefOf.Sandstone.building.naturalTerrain;
            return GenStep_RocksFromGrid.RockDefAt(new IntVec3((int) Math.Round(x, 0), 0, (int) Math.Round(z, 0))).building.naturalTerrain;
        }

        public override string ToString() => "ROCK GRID {}";
    }
}

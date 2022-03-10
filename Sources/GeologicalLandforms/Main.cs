using System.Collections.Generic;
using GeologicalLandforms.TerrainGraph;
using HarmonyLib;
using NodeEditorFramework;
using Verse;

namespace GeologicalLandforms;

[StaticConstructorOnStartup]
public static class Main
{
    public static readonly IReadOnlyCollection<string> ExcludedBiomePrefixes = new HashSet<string>
    {
        "BiomesIslands",
        "BMT_FungalForest"
    };

    static Main()
    {
        new Harmony("Geological Landforms").PatchAll();
        NodeEditor.ReInit(false);
        LandformManager.InitialLoad();
    }

    public static Rot6 Random(this List<Rot6> rotList, int tileId)
    {
        if (rotList.Count == 0) return Rot6.Invalid;
        return rotList[Rand.RangeSeeded(0, rotList.Count, tileId)];
    }

}
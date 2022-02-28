using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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

    public static Settings Settings;

    static Main() => new Harmony("Geological Landforms").PatchAll();
    
    public static Rot6 Random(this List<Rot6> rotList, int tileId)
    {
        if (rotList.Count == 0) return Rot6.Invalid;
        return rotList[Rand.RangeSeeded(0, rotList.Count, tileId)];
    }

}
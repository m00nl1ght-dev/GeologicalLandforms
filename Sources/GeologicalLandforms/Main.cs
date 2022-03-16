using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using RimWorld;
using TerrainGraph;
using UnityEngine;
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
        
        AssetBundle assetBundle = ModInstance.ModContentPack.assetBundles.loadedAssetBundles.Find(b => b.name == "terraingraph");
        ResourceManager.InitAssetBundle(assetBundle);

        NodeBase.ActiveDropdownHandler = (values, action) =>
        {
            List<FloatMenuOption> options = values.Select((e, i) => new FloatMenuOption(e, () => { action(i); })).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        };

        NodeBase.ActiveTooltipHandler = (rect, textFunc, tdelay) =>
        {
            TooltipHandler.TipRegion(rect, new TipSignal(textFunc, textFunc.GetHashCode()) {delay = tdelay});
        };
        
        NodeEditor.ReInit(false);
        LandformManager.InitialLoad();
    }

    public static Rot6 Random(this List<Rot6> rotList, int tileId)
    {
        if (rotList.Count == 0) return Rot6.Invalid;
        return rotList[Rand.RangeSeeded(0, rotList.Count, tileId)];
    }
}
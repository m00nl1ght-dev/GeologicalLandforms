using System.Linq;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

// ReSharper disable All
namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof(World))]
internal static class RimWorld_World
{
    public static int LastKnownInitialWorldSeed { get; private set; }
    
    [HarmonyPatch(nameof(World.ConstructComponents))]
    private static void Postfix(WorldInfo ___info)
    {
        LastKnownInitialWorldSeed = ___info.Seed;
    }
    
    [HarmonyAfter("zylle.MapDesigner")]
    [HarmonyPatch(nameof(World.HasCaves))]
    private static bool Prefix(ref bool __result, int tile)
    {
        WorldTileInfo worldTileInfo = WorldTileInfo.Get(tile);
        Landform landform = worldTileInfo.Landforms?.FirstOrDefault(l => !l.IsLayer);
        if (landform == null) return true;

        int seed = worldTileInfo.MakeSeed(8266);
        __result = Rand.ChanceSeeded(landform.WorldTileReq.CaveChance, seed);
        return false;
    }
}
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
    
    [HarmonyPriority(-10)]
    [HarmonyPatch(nameof(World.HasCaves))]
    private static void Postfix(ref bool __result, int tile)
    {
        WorldTileInfo worldTileInfo = WorldTileInfo.Get(tile);
        if (worldTileInfo.Landform == null) return;

        int seed = worldTileInfo.MakeSeed(8266);
        __result = Rand.ChanceSeeded(worldTileInfo.Landform.WorldTileReq.CaveChance, seed);
    }
}
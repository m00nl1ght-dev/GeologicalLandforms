using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(World))]
internal static class Patch_RimWorld_World
{
    public static int LastKnownInitialWorldSeed { get; private set; }

    public static WeakReference LastFinalizedWorldRef { get; private set; }

    public static int StableSeedForTile(int tileId) => Gen.HashCombineInt(LastKnownInitialWorldSeed, tileId);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(World.ConstructComponents))]
    private static void ConstructComponents(WorldInfo ___info, World __instance)
    {
        LastKnownInitialWorldSeed = ___info.Seed;

        var landformData = __instance.GetComponent<LandformData>();

        var lastWorld = Patch_RimWorld_WorldGenStep_Terrain.LastWorld;
        var caveSystems = Patch_RimWorld_WorldGenStep_Terrain.CaveSystems;
        var biomeTransitions = Patch_RimWorld_WorldGenStep_Terrain.BiomeTransitions;

        if (landformData != null && lastWorld == __instance)
        {
            landformData.SetCaveSystems(caveSystems);
            landformData.SetBiomeTransitions(biomeTransitions);
            Patch_RimWorld_WorldGenStep_Terrain.BiomeTransitions = null;
            Patch_RimWorld_WorldGenStep_Terrain.CaveSystems = null;
            Patch_RimWorld_WorldGenStep_Terrain.LastWorld = null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.FinalizeInit))]
    private static void FinalizeInit(World __instance)
    {
        LastFinalizedWorldRef = new WeakReference(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.HasCaves))]
    [HarmonyAfter("zylle.MapDesigner")]
    [PatchExcludedFromConflictCheck]
    private static bool HasCaves(World __instance, int tile, ref bool __result)
    {
        if (!__instance.HasFinishedGenerating()) return true;
        
        var worldTileInfo = WorldTileInfo.Get(tile);
        var landform = worldTileInfo.Landforms?.LastOrDefault(l => !l.IsLayer);

        if (landform == null) return true;

        if (landform.WorldTileReq != null)
        {
            int seed = worldTileInfo.MakeSeed(8266);
            __result = Rand.ChanceSeeded(landform.WorldTileReq.CaveChance, seed);
        }
        else
        {
            __result = landform.OutputCaves != null;
        }

        return false;
    }

    [ThreadStatic]
    private static Tile _rockCacheTile;

    [ThreadStatic]
    private static IEnumerable<ThingDef> _rockCacheValue;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.Last)]
    [PatchExcludedFromConflictCheck]
    private static bool NaturalRockTypesIn_Prefix(World __instance, int tile, out bool __state)
    {
        // NaturalRockTypesIn is called many times during map generation, and also while a deep drill is running.
        // It's not like the rock types for a tile ever change, so it's much more efficient to cache this and only calculate once.
        // The patch priority of the prefix is the lowest possible, so that other mods can override the cache
        // in case they want to change the result in their own prefix.
        __state = _rockCacheTile == __instance.grid[tile];
        return !__state;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.First)]
    private static void NaturalRockTypesIn_Postfix(World __instance, int tile, ref bool __state, ref IEnumerable<ThingDef> __result)
    {
        if (__state)
        {
            // Cache match, return the already computed list.
            __result = _rockCacheValue;
            return;
        }

        try
        {
            if (__instance.HasFinishedGenerating() && __result is List<ThingDef> list)
            {
                var worldTileInfo = WorldTileInfo.Get(tile);
                var xmlTileContext = new CtxTile(worldTileInfo);
                var biomeProperties = worldTileInfo.Biome.Properties();

                var fromBiome = biomeProperties.worldTileOverrides?.stoneTypes;
                if (fromBiome != null)
                {
                    list = fromBiome.Get(xmlTileContext, list);
                }

                if (worldTileInfo.HasBiomeVariants)
                {
                    foreach (var biomeVariant in worldTileInfo.BiomeVariants)
                    {
                        var fromBiomeVariant = biomeVariant.worldTileOverrides?.stoneTypes;
                        if (fromBiomeVariant != null)
                        {
                            list = fromBiomeVariant.Get(xmlTileContext, list);
                        }
                    }
                }

                list.RemoveAll(r => r == null);
                if (list.Count > 0) __result = list;
            }
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Error in NaturalRockTypesIn for tile " + tile, e);
        }

        // Store the result in the cache.
        _rockCacheTile = __instance.grid[tile];
        _rockCacheValue = __result;
    }
}

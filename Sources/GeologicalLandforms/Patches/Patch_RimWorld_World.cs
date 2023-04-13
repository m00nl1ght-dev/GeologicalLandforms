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

    public static int StableSeedForTile(int tileId) => Gen.HashCombineInt(LastKnownInitialWorldSeed, tileId);

    [HarmonyPostfix]
    [HarmonyPatch("ConstructComponents")]
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
    [HarmonyPatch(nameof(World.HasCaves))]
    [HarmonyAfter("zylle.MapDesigner")]
    [PatchExcludedFromConflictCheck]
    private static bool HasCaves(ref bool __result, int tile)
    {
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
    private static int _rockCacheTile;

    [ThreadStatic]
    private static IEnumerable<ThingDef> _rockCacheValue;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.First)]
    private static void NaturalRockTypesIn_Postfix(ref IEnumerable<ThingDef> __result, int tile)
    {
        try
        {
            if (__result is List<ThingDef> list)
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

        // NaturalRockTypesIn is called many times during map generation, and also while any deep drill is running.
        // It's not like the rock types for a tile ever change, so it's much more efficient to cache this and only calculate once.
        // This postfix stores the result in a thread-safe field and the prefix below returns when the tile matches.
        // The patch priority of the prefix is the lowest possible, so that other mods can override the cache
        // in case they want to change the result in their own prefix.
        _rockCacheValue = __result;
        _rockCacheTile = tile;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.Last)]
    [PatchExcludedFromConflictCheck]
    private static bool NaturalRockTypesIn_Prefix(ref IEnumerable<ThingDef> __result, int tile)
    {
        if (_rockCacheTile == tile && _rockCacheValue != null)
        {
            __result = _rockCacheValue;
            return false;
        }

        return true;
    }
}

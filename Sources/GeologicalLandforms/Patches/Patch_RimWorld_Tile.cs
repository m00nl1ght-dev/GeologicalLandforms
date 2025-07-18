#if RW_1_6_OR_GREATER

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Tile))]
internal static class Patch_RimWorld_Tile
{
    [HarmonyPostfix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch(nameof(Tile.Mutators), MethodType.Getter)]
    private static void GetMutators_Postfix(Tile __instance, ref IList<TileMutatorDef> __result)
    {
        if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0)
        {
            __result = TileMutatorsCustomization.Get(__instance.tile.tileId, __result);
        }
    }

    [HarmonyPostfix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch(nameof(Tile.Biomes), MethodType.Getter)]
    private static void GetBiomes_Postfix(Tile __instance, ref IEnumerable<BiomeDef> __result)
    {
        if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0)
        {
            var tileInfo = WorldTileInfo.Get(__instance.tile.tileId);
            if (tileInfo.HasBorderingBiomes())
            {
                __result = __result.Concat(tileInfo.BorderingBiomes.Select(b => b.Biome)).Distinct();
            }
        }
    }

    [HarmonyPostfix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch(nameof(Tile.AnimalDensity), MethodType.Getter)]
    private static void AnimalDensity_Postfix(Tile __instance, ref float __result)
    {
        if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0)
        {
            var tileInfo = WorldTileInfo.Get(__instance.tile.tileId);
            if (tileInfo.HasLandforms())
            {
                var biomeGrid = Find.Maps.Find(m => m.Tile == __instance.tile)?.BiomeGrid();
                if (biomeGrid != null)
                {
                    __result *= GeologicalLandformsAPI.AnimalDensityFactor.Apply(biomeGrid);
                }
            }
        }
    }

    [HarmonyPostfix]
    [PatchTargetPotentiallyInlined]
    [HarmonyPatch(nameof(Tile.AllowRoofedEdgeWalkIn), MethodType.Getter)]
    private static void AllowRoofedEdgeWalkIn_Postfix(Tile __instance, ref bool __result)
    {
        if (__instance.Layer.IsRootSurface && __instance.hilliness == Hilliness.Impassable)
            __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Tile.AddMutator))]
    private static bool AddMutator_Prefix(Tile __instance, TileMutatorDef mutator)
    {
        if (mutator.Worker is TileMutatorWorker_Landform worker)
        {
            if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0 && worker.Landform != null)
            {
                var tileInfo = WorldTileInfo.Get(__instance.tile.tileId);
                var landforms = tileInfo.Landforms?.Select(lf => lf.Id).ToList() ?? [];

                if (!landforms.Contains(worker.Landform.Id))
                {
                    landforms.Add(worker.Landform.Id);

                    Find.World.LandformData()?.Commit(__instance.tile.tileId, new LandformData.TileData(tileInfo)
                    {
                        Landforms = landforms
                    });
                }
            }

            return false;
        }

        if (__instance.Layer.IsRootSurface && __instance.mutatorsNullable is {} existingList)
        {
            if (TileMutatorsCustomization.Enabled)
            {
                for (int i = existingList.Count - 1; i >= 0; i--)
                {
                    var existing = existingList[i];

                    if (mutator.categories.Any(c => existing.categories.Contains(c)))
                    {
                        if (TileMutatorsCustomization.IsTileMutatorDisabled(existing))
                        {
                            existingList.Remove(existing);
                        }
                    }
                }
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Tile.RemoveMutator))]
    private static bool RemoveMutator_Prefix(Tile __instance, TileMutatorDef mutator)
    {
        if (mutator.Worker is TileMutatorWorker_Landform worker)
        {
            if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0 && worker.Landform != null)
            {
                var tileInfo = WorldTileInfo.Get(__instance.tile.tileId);
                var landforms = tileInfo.Landforms?.Select(lf => lf.Id).ToList() ?? [];

                if (landforms.Contains(worker.Landform.Id))
                {
                    landforms.Remove(worker.Landform.Id);

                    Find.World.LandformData()?.Commit(__instance.tile.tileId, new LandformData.TileData(tileInfo)
                    {
                        Landforms = landforms
                    });
                }
            }

            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tile.AddMutator))]
    private static void AddMutator_Postfix(Tile __instance)
    {
        if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0)
            TileMutatorsCustomization.TileHasChanged(__instance.tile.tileId);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tile.RemoveMutator))]
    private static void RemoveMutator_Postfix(Tile __instance)
    {
        if (__instance.Layer.IsRootSurface && __instance.tile.tileId >= 0)
            TileMutatorsCustomization.TileHasChanged(__instance.tile.tileId);
    }
}

#endif

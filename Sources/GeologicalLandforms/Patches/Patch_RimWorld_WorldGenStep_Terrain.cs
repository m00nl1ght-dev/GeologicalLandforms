using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldGenStep_Terrain))]
internal static class Patch_RimWorld_WorldGenStep_Terrain
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WorldGenStep_Terrain);

    private static readonly List<int> _tmpNeighbors = new();

    internal static World LastWorld;
    internal static bool[] BiomeTransitions;

    [HarmonyPostfix]
    [HarmonyPatch("GenerateGridIntoWorld")]
    private static void GenerateGridIntoWorld_Postfix(WorldGenStep_Terrain __instance)
    {
        try
        {
            CalcTransitions(__instance);
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to calculate biome transitions.", e);
            BiomeTransitions = null;
            LastWorld = null;
        }
    }

    private static void CalcTransitions(WorldGenStep_Terrain instance)
    {
        var world = Find.World;
        var grid = world.grid;

        int tilesCount = grid.TilesCount;
        var data = new bool[tilesCount * 6];

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = grid[tileIdx];
            var biome = tile.biome;

            grid.GetTileNeighbors(tileIdx, _tmpNeighbors);

            for (var i = 0; i < _tmpNeighbors.Count; i++)
            {
                int nIdx = _tmpNeighbors[i];
                var nTile = grid[nIdx];
                var nBiome = nTile.biome;

                if (biome != nBiome)
                {
                    data[tileIdx * 6 + i] = CheckIsTransition(instance, tileIdx, nIdx, biome, nBiome);
                }
            }
        }

        BiomeTransitions = data;
        LastWorld = world;

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Calculation of biome transitions took " + stopwatch.ElapsedMilliseconds + " ms.");
    }

    private static bool CheckIsTransition(WorldGenStep_Terrain instance, int tile, int nTile, BiomeDef biome, BiomeDef nBiome)
    {
        bool rev = tile > nTile;
        int min = rev ? nTile : tile;
        int max = rev ? tile : nTile;

        Rand.PushState(Gen.HashCombineInt(min, max));

        bool flag = Rand.Bool ^ rev;
        var rTile = flag ? nTile : tile;

        var grid = Find.WorldGrid;
        var pos = Vector3.Lerp(grid.GetTileCenter(min), grid.GetTileCenter(max), 0.5f);

        var tTileInfo = GenerateTileFor_AtVector3(instance, -1, pos);

        var diff = nBiome.Worker.GetScore(tTileInfo, rTile) - biome.Worker.GetScore(tTileInfo, rTile);

        Rand.PopState();

        if (diff > 0) return true;
        if (diff < 0) return false;

        return flag;
    }

    [HarmonyPatch("GenerateTileFor")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static Tile GenerateTileFor_AtVector3(WorldGenStep_Terrain instance, int tileID, Vector3 pos)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var useGivenPos = TranspilerPattern.Build("UseGivenPos")
                .MatchCall(typeof(Find), "get_WorldGrid").Nop()
                .Match(OpCodes.Ldarg_1).Remove()
                .MatchCall(typeof(WorldGrid), "GetTileCenter").Remove()
                .Insert(OpCodes.Ldarg_2)
                .MatchStloc().Keep();

            var longLatOf = TranspilerPattern.Build("LongLatOf")
                .MatchCall(typeof(Find), "get_WorldGrid").Nop()
                .Match(OpCodes.Ldarg_1).Remove()
                .MatchCall(typeof(WorldGrid), "LongLatOf").Remove()
                .Insert(OpCodes.Ldarg_2)
                .Insert(CodeInstruction.Call(Self, nameof(LongLatOf_Vector3)))
                .Greedy();

            var skipBiome = TranspilerPattern.Build("SkipBiome")
                .MatchLdloc().Nop()
                .Match(OpCodes.Ldarg_0).Remove()
                .MatchLdloc().Remove()
                .Match(OpCodes.Ldarg_1).Remove()
                .Match(OpCodes.Call).Remove()
                .MatchStore(typeof(Tile), "biome").Remove();

            return TranspilerPattern.Apply(instructions, useGivenPos, longLatOf, skipBiome);
        }

        _ = Transpiler(null);
        return null;
    }

    private static Vector2 LongLatOf_Vector3(Vector3 pos)
    {
        return new Vector2(Mathf.Atan2(pos.x, -pos.z) * 57.29578f, Mathf.Asin(pos.y / 100f) * 57.29578f);
    }
}

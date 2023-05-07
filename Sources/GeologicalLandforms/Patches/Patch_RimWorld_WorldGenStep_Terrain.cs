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
using Verse.Noise;
using static GeologicalLandforms.WorldTileTraverser;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldGenStep_Terrain))]
internal static class Patch_RimWorld_WorldGenStep_Terrain
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WorldGenStep_Terrain);

    internal static float CaveSystemNoiseThreshold = -0.3f;
    internal static float HillinessNoiseOffset = 0f;

    internal static World LastWorld;
    internal static bool[] BiomeTransitions;
    internal static byte[] CaveSystems;

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
            GeologicalLandformsAPI.Logger.Error("Failed to calculate extended biome data.", e);
            BiomeTransitions = null;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetupHillinessNoise")]
    private static void SetupHillinessNoise_Postfix(ref ModuleBase ___noiseMountainLines, ModuleBase ___noiseElevation)
    {
        if (HillinessNoiseOffset != 0f)
        {
            ___noiseMountainLines = new Add(___noiseMountainLines, new Const(HillinessNoiseOffset));
        }
        
        try
        {
            CalcCaveSystems(___noiseMountainLines, ___noiseElevation);
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to calculate cave systems.", e);
            CaveSystems = null;
        }
    }

    private static void CalcCaveSystems(ModuleBase hillinessModule, ModuleBase elevationModule)
    {
        var world = Find.World;
        var grid = world.grid;

        int tilesCount = grid.TilesCount;
        var data = new byte[tilesCount];

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var traverser = new WorldTileTraverser();
        var noiseSeed = Gen.HashCombineInt(world.info.Seed, 863332822);

        const int minSize = 4;
        const int maxDist = 10;

        const double impassableThrshold = 0.03629999980330467;

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tileCenter = grid.GetTileCenter(tileIdx);

            if (hillinessModule.GetValue(tileCenter) > impassableThrshold) continue;
            if (!InCaveSystemNoiseThreshold(tileIdx, tileCenter, noiseSeed)) continue;
            if (elevationModule.GetValue(tileCenter) <= 0) continue;

            var dist = 99;
            var size = 1;

            var success = traverser.Traverse(grid, tileIdx, maxDist, (t, d) =>
            {
                var tc = grid.GetTileCenter(t);

                if (hillinessModule.GetValue(tc) > impassableThrshold)
                {
                    if (d < dist) dist = d;
                    return size >= minSize ? TraverseAction.Stop : TraverseAction.Ignore;
                }

                if (InCaveSystemNoiseThreshold(t, tc, noiseSeed))
                {
                    size++;
                    return dist <= maxDist && size >= minSize ? TraverseAction.Stop : TraverseAction.Pass;
                }

                return TraverseAction.Ignore;
            });

            if (success) data[tileIdx] = (byte) dist;
        }

        CaveSystems = data;
        LastWorld = world;

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Calculation of cave systems took " + stopwatch.ElapsedMilliseconds + " ms.");
    }

    private static bool InCaveSystemNoiseThreshold(int tileId, Vector3 pos, int seed)
    {
        var perlin = Perlin.GetValue(pos.x, pos.y, pos.z, 0.35f, seed, 2f, 0.5f, 6, QualityMode.Low);
        return perlin > CaveSystemNoiseThreshold && Rand.ChanceSeeded(0.5f, Gen.HashCombineInt(seed, tileId));
    }

    private static void CalcTransitions(WorldGenStep_Terrain instance)
    {
        var world = Find.World;
        var grid = world.grid;

        int tilesCount = grid.TilesCount;
        var data = new bool[tilesCount * 6];

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var nbData = grid.tileIDToNeighbors_values;
        var nbOffsets = grid.tileIDToNeighbors_offsets;

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = grid[tileIdx];
            var biome = tile.biome;

            var nbOffset = nbOffsets[tileIdx];
            var nbBound = nbOffsets.IdxBoundFor(nbData, tileIdx);

            for (var i = nbOffset; i < nbBound; i++)
            {
                int nIdx = nbData[i];
                var nTile = grid[nIdx];
                var nBiome = nTile.biome;

                if (biome != nBiome)
                {
                    data[tileIdx * 6 + i - nbOffset] = CheckIsTransition(instance, tileIdx, nIdx, biome, nBiome);
                }
            }
        }

        BiomeTransitions = data;
        LastWorld = world;

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Calculation of extended biome data took " + stopwatch.ElapsedMilliseconds + " ms.");
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

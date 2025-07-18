using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Threading;
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

    #if !RW_1_6_OR_GREATER

    internal static World LastWorld;
    internal static bool[] BiomeTransitions;
    internal static byte[] CaveSystems;

    #endif

    [HarmonyPostfix]
    #if RW_1_6_OR_GREATER
    [HarmonyPatch("GenerateFresh")]
    #else
    [HarmonyPatch("GenerateGridIntoWorld")]
    #endif
    private static void GenerateFresh_Postfix(WorldGenStep_Terrain __instance)
    {
        try
        {
            CalcTransitions(__instance);
        }
        catch (ThreadAbortException) { throw; }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to calculate extended biome data.", e);
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
        catch (ThreadAbortException) { throw; }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to calculate cave systems.", e);
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

        const double impassableThreshold = 0.03629999980330467;

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tileCenter = grid.GetTileCenter(tileIdx);

            if (hillinessModule.GetValue(tileCenter) > impassableThreshold) continue;
            if (!InCaveSystemNoiseThreshold(tileIdx, tileCenter, noiseSeed)) continue;
            if (elevationModule.GetValue(tileCenter) <= 0) continue;

            var dist = 99;
            var size = 1;

            var success = traverser.Traverse(grid, tileIdx, maxDist, (t, d) =>
            {
                var tc = grid.GetTileCenter(t);

                if (hillinessModule.GetValue(tc) > impassableThreshold)
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

        #if RW_1_6_OR_GREATER
        world.LandformData()?.SetCaveSystems(data);
        #else
        CaveSystems = data;
        LastWorld = world;
        #endif

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Calculation of cave systems took " + stopwatch.ElapsedMilliseconds + " ms.");
    }

    private static bool InCaveSystemNoiseThreshold(int tileId, Vector3 pos, int seed)
    {
        #if RW_1_6_OR_GREATER
        var perlin = Perlin.GetValue(pos.x, pos.y, pos.z, 0.35f, seed, 2f, 0.5f, 6, false, false, QualityMode.Low);
        #else
        var perlin = Perlin.GetValue(pos.x, pos.y, pos.z, 0.35f, seed, 2f, 0.5f, 6, QualityMode.Low);
        #endif

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

        var nbData = grid.ExtNbValues();
        var nbOffsets = grid.ExtNbOffsets();

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

        #if RW_1_6_OR_GREATER
        world.LandformData()?.SetBiomeTransitions(data);
        #else
        BiomeTransitions = data;
        LastWorld = world;
        #endif

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

        #if RW_1_6_OR_GREATER
        var tTileInfo = GenerateTileFor_AtVector3(instance, PlanetTile.Invalid, null, pos);
        var diff = nBiome.Worker.GetScore(nBiome, tTileInfo, rTile) - biome.Worker.GetScore(biome, tTileInfo, rTile);
        #else
        var tTileInfo = GenerateTileFor_AtVector3(instance, -1, pos);
        var diff = nBiome.Worker.GetScore(tTileInfo, rTile) - biome.Worker.GetScore(tTileInfo, rTile);
        #endif

        Rand.PopState();

        if (diff > 0) return true;
        if (diff < 0) return false;

        return flag;
    }

    #if RW_1_6_OR_GREATER

    [HarmonyPatch("GenerateTileFor")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow + 1)]
    private static Tile GenerateTileFor_AtVector3(WorldGenStep_Terrain instance, PlanetTile tile, PlanetLayer layer, Vector3 pos)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var useGivenPos = TranspilerPattern.Build("UseGivenPos")
                .Match(OpCodes.Ldarg_2).Nop()
                .Match(OpCodes.Ldarg_1).Remove()
                .MatchCall(typeof(PlanetLayer), "GetTileCenter", [typeof(PlanetTile)]).Remove()
                .Insert(OpCodes.Ldarg_3)
                .MatchStloc().Keep();

            var longLatOf = TranspilerPattern.Build("LongLatOf")
                .Match(OpCodes.Ldarg_2).Nop()
                .Match(OpCodes.Ldarg_1).Remove()
                .MatchCall(typeof(PlanetLayer), "LongLatOf", [typeof(PlanetTile)]).Remove()
                .Insert(OpCodes.Ldarg_3)
                .Insert(CodeInstruction.Call(Self, nameof(LongLatOf_Vector3)))
                .Greedy();

            var skipBiome = TranspilerPattern.Build("SkipBiome")
                .MatchLdloc().Nop()
                .Match(OpCodes.Ldarg_0).Remove()
                .MatchLdloc().Remove()
                .Match(OpCodes.Ldarg_1).Remove()
                .Match(OpCodes.Ldarg_2).Remove()
                .Match(OpCodes.Call).Remove()
                .MatchCall(typeof(Tile), "set_PrimaryBiome").Remove();

            return TranspilerPattern.Apply(instructions, useGivenPos, longLatOf, skipBiome);
        }

        _ = Transpiler(null);
        return null;
    }

    #else

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

    #endif

    private static Vector2 LongLatOf_Vector3(Vector3 pos)
    {
        return new Vector2(Mathf.Atan2(pos.x, -pos.z) * 57.29578f, Mathf.Asin(pos.y / 100f) * 57.29578f);
    }
}

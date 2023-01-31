using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldGenStep_Terrain))]
internal static class Patch_RimWorld_WorldGenStep_Terrain
{
    private static ModuleBase noiseElevation;
    private static ModuleBase noiseTemperatureOffset;
    private static ModuleBase noiseRainfall;
    private static ModuleBase noiseSwampiness;

    private static SimpleCurve AvgTempByLatitudeCurve;

    private static readonly List<int> _tmpNeighbors = new();

    internal static World LastWorld;
    internal static bool[] BiomeTransitions;

    [HarmonyPostfix]
    [HarmonyPatch("GenerateGridIntoWorld")]
    private static void GenerateGridIntoWorld(
        ModuleBase ___noiseElevation,
        ModuleBase ___noiseTemperatureOffset,
        ModuleBase ___noiseRainfall,
        ModuleBase ___noiseSwampiness,
        SimpleCurve ___AvgTempByLatitudeCurve)
    {
        AvgTempByLatitudeCurve = ___AvgTempByLatitudeCurve;
        noiseElevation = ___noiseElevation;
        noiseTemperatureOffset = ___noiseTemperatureOffset;
        noiseRainfall = ___noiseRainfall;
        noiseSwampiness = ___noiseSwampiness;

        try
        {
            CalcTransitions();
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to calculate biome transitions.", e);
            BiomeTransitions = null;
            LastWorld = null;
        }

        noiseElevation = null;
        noiseTemperatureOffset = null;
        noiseRainfall = null;
        noiseSwampiness = null;
    }

    private static void CalcTransitions()
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
                    data[tileIdx * 6 + i] = CheckIsTransition(tileIdx, nIdx, biome, nBiome);
                }
            }
        }

        BiomeTransitions = data;
        LastWorld = world;

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("Patch_RimWorld_WorldGenStep_Terrain took " + stopwatch.ElapsedMilliseconds + " ms.");
    }

    private static bool CheckIsTransition(int tile, int nTile, BiomeDef biome, BiomeDef nBiome)
    {
        bool rev = tile > nTile;
        int min = rev ? nTile : tile;
        int max = rev ? tile : nTile;

        Rand.PushState(Gen.HashCombineInt(min, max));

        bool flag = Rand.Bool ^ rev;
        var rTile = flag ? nTile : tile;

        var grid = Find.WorldGrid;
        var hilliness = grid[rTile].hilliness;
        var pos = Vector3.Lerp(grid.GetTileCenter(min), grid.GetTileCenter(max), 0.5f);

        var tTileInfo = GenerateTileFor(pos, hilliness);

        var diff = nBiome.Worker.GetScore(tTileInfo, rTile) - biome.Worker.GetScore(tTileInfo, rTile);

        Rand.PopState();

        if (diff > 0) return true;
        if (diff < 0) return false;

        return flag;
    }

    private static Tile GenerateTileFor(Vector3 pos, Hilliness hilliness)
    {
        var ws = new Tile
        {
            hilliness = hilliness,
            elevation = noiseElevation.GetValue(pos)
        };

        var longLat = new Vector2(Mathf.Atan2(pos.x, -pos.z) * 57.29578f, Mathf.Asin(pos.y / 100f) * 57.29578f);
        float x = BaseTemperatureAtLatitude(longLat.y) - TemperatureReductionAtElevation(ws.elevation) + noiseTemperatureOffset.GetValue(pos);

        var temperatureCurve = Find.World.info.overallTemperature.GetTemperatureCurve();
        if (temperatureCurve != null)
            x = temperatureCurve.Evaluate(x);

        ws.temperature = x;
        ws.rainfall = noiseRainfall.GetValue(pos);

        if (ws.hilliness is Hilliness.Flat or Hilliness.SmallHills)
            ws.swampiness = noiseSwampiness.GetValue(pos);

        return ws;
    }

    private static float BaseTemperatureAtLatitude(float lat)
    {
        float x = Mathf.Abs(lat) / 90f;
        return AvgTempByLatitudeCurve.Evaluate(x);
    }

    private static float TemperatureReductionAtElevation(float elev) => elev < 250.0 ? 0.0f : Mathf.Lerp(0.0f, 40f, (float) ((elev - 250.0) / 4750.0));
}

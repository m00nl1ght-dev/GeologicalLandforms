using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldGenStep_Terrain))]
internal static class Patch_RimWorld_WorldGenStep_Terrain
{
    private static ModuleBase noiseElevation;
    private static ModuleBase noiseTemperatureOffset;
    private static ModuleBase noiseRainfall;
    private static ModuleBase noiseSwampiness;
    private static ModuleBase noiseMountainLines;
    private static ModuleBase noiseHillsPatchesMicro;
    private static ModuleBase noiseHillsPatchesMacro;
  
    [HarmonyPostfix]
    [HarmonyPatch("GenerateGridIntoWorld")]
    private static void GenerateGridIntoWorld(
        ModuleBase ___noiseElevation,
        ModuleBase ___noiseTemperatureOffset,
        ModuleBase ___noiseRainfall,
        ModuleBase ___noiseSwampiness,
        ModuleBase ___noiseMountainLines,
        ModuleBase ___noiseHillsPatchesMicro,
        ModuleBase ___noiseHillsPatchesMacro)
    {
        noiseElevation = ___noiseElevation;
        noiseTemperatureOffset = ___noiseTemperatureOffset;
        noiseRainfall = ___noiseRainfall;
        noiseSwampiness = ___noiseSwampiness;
        noiseMountainLines = ___noiseMountainLines;
        noiseHillsPatchesMicro = ___noiseHillsPatchesMicro;
        noiseHillsPatchesMacro = ___noiseHillsPatchesMacro;
    }
    
    public static Tile GenerateTileFor(Vector3 pos, Hilliness hilliness)
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

    private static readonly SimpleCurve AvgTempByLatitudeCurve = new()
    {
        new CurvePoint(0.0f, 30f),
        new CurvePoint(0.1f, 29f),
        new CurvePoint(0.5f, 7f),
        new CurvePoint(1f, -37f)
    };
}
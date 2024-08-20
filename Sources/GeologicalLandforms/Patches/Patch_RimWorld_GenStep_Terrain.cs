using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using LunarFramework.Utility;
using MapPreview;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class Patch_RimWorld_GenStep_Terrain
{
    private static readonly Type Self = typeof(Patch_RimWorld_GenStep_Terrain);

    public static IGridFunction<TerrainDef> BaseFunction { get; private set; }
    public static IGridFunction<TerrainDef> StoneFunction { get; private set; }
    public static IGridFunction<TerrainDef> RiverFunction { get; private set; }
    public static IGridFunction<BiomeDef> BiomeFunction { get; private set; }

    public static bool UseVanillaTerrain { get; private set; } = true;
    public static bool DebugWarnedMissingTerrain { get; private set; }

    private static BiomeGrid _biomeGrid;

    [HarmonyPrefix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void Generate_Prefix(Map map, GenStepParams parms)
    {
        Init(map.BiomeGrid());
    }

    [HarmonyPostfix]
    [HarmonyPatch("Generate")]
    [HarmonyPriority(Priority.Last)]
    private static void Generate_Postfix(Map map, GenStepParams parms)
    {
        ApplyWaterFlow(map);
        CleanUp();
    }

    [HarmonyPrefix]
    [HarmonyPatch("TerrainFrom")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool TerrainFrom(ref TerrainDef __result, IntVec3 c, Map map, float elevation, float fertility, RiverMaker river, bool preferSolid)
    {
        if (UseVanillaTerrain) return true;

        var tRiver = river?.TerrainAt(c, true);
        __result = TerrainAt(c, map, elevation, fertility, tRiver, preferSolid);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("GenerateRiver")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool GenerateRiver_Prefix()
    {
        return RiverFunction == null;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("GenerateRiver")]
    [HarmonyPriority(Priority.VeryLow)]
    private static IEnumerable<CodeInstruction> GenerateRiver_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ldlocAngle = new CodeInstruction(OpCodes.Ldloc);

        var findLoc = TranspilerPattern.Build("FindRiverAngleLoc")
            .MatchCall(typeof(WorldGrid), "GetHeadingFromTo", [typeof(int), typeof(int)]).Keep()
            .MatchStloc().StoreOperandIn(ldlocAngle).Keep();

        var injectOpt = TranspilerPattern.Build("OptimizeRiverAngleIfNeeded")
            .OnlyMatchAfter(findLoc)
            .MatchNewobj(typeof(Vector3), [typeof(float), typeof(float), typeof(float)]).Keep()
            .Insert(OpCodes.Ldarg_1)
            .Insert(CodeInstruction.Call(Self, nameof(OptimizeVanillaRiverCenterIfNeeded)))
            .Match(ldlocAngle).Keep()
            .Insert(CodeInstruction.Call(Self, nameof(OptimizeVanillaRiverAngleIfNeeded)));

        return TranspilerPattern.Apply(instructions, findLoc, injectOpt);
    }

    public static void Init(BiomeGrid biomeGrid)
    {
        CleanUp();

        var tile = Landform.GeneratingTile as WorldTileInfo;
        var mapSize = Landform.GeneratingMapSize;

        if (tile == null) return;

        BaseFunction = Landform.GetFeatureScaled(l => l.OutputTerrain?.GetBase());
        StoneFunction = Landform.GetFeatureScaled(l => l.OutputTerrain?.GetStone());
        RiverFunction = Landform.GetFeatureScaled(l => l.OutputWaterFlow?.GetRiverTerrain());
        BiomeFunction = Landform.GetFeatureScaled(l => l.OutputBiomeGrid?.GetBiomeGrid());

        _biomeGrid = biomeGrid;

        bool hasBiomeTransition = false;
        if (tile.HasBorderingBiomes())
        {
            var transition = Landform.GetFeature(l => l.OutputBiomeGrid?.ApplyBiomeTransitions(tile, mapSize, BiomeFunction));
            if (transition != null)
            {
                BiomeFunction = transition;
                hasBiomeTransition = true;
            }
        }

        if (_biomeGrid != null && BiomeFunction != null)
        {
            _biomeGrid.Enabled = true;
            _biomeGrid.SetBiomes(BiomeFunction);

            if (hasBiomeTransition)
            {
                BiomeTransition.PostProcessBiomeGrid(_biomeGrid, mapSize);
            }
        }

        UseVanillaTerrain = ShouldUseVanillaTerrain(tile);
    }

    private static bool ShouldUseVanillaTerrain(WorldTileInfo tile)
    {
        if (BaseFunction != null || StoneFunction != null || RiverFunction != null || BiomeFunction != null) return false;
        if (tile.Biome.Properties().gravelTerrain != null) return false;
        return true;
    }

    public static void CleanUp()
    {
        UseVanillaTerrain = true;
        DebugWarnedMissingTerrain = false;
        BaseFunction = null;
        StoneFunction = null;
        RiverFunction = null;
        BiomeFunction = null;
        _biomeGrid = null;
    }

    public static TerrainDef TerrainAt(IntVec3 c, Map map, float elevation, float fertility, TerrainDef tRiver, bool preferSolid)
    {
        if (RiverFunction != null) tRiver = RiverFunction.ValueAt(c.x, c.z);

        if (tRiver == null && preferSolid)
        {
            return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
        }

        var tBase = BaseFunction?.ValueAt(c.x, c.z);

        if (tBase.IsDeepWater()) return tBase;

        if (RiverFunction != null)
        {
            if (tRiver == TerrainDefOf.WaterMovingChestDeep) return tRiver;
            if (tBase == TerrainDefOf.WaterShallow || tBase == TerrainDefOf.WaterOceanShallow) return tBase;
        }

        if (tRiver is { IsRiver: true }) return tRiver;

        if (tBase != null) return tBase;
        if (tRiver != null) return tRiver;

        var biome = _biomeGrid?.BiomeAt(c) ?? Landform.GeneratingTile?.Biome ?? map.Biome;
        foreach (var patchMaker in biome.terrainPatchMakers)
        {
            var tPatch = patchMaker.TerrainAt(c, map, fertility);
            if (tPatch != null) return tPatch;
        }

        var tStone = StoneFunction == null ? DefaultElevationTerrain(c, biome, elevation) : StoneFunction.ValueAt(c.x, c.z);
        if (tStone != null) return tStone;

        var tBiome = TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, fertility);
        if (tBiome != null) return tBiome;

        if (!DebugWarnedMissingTerrain)
        {
            Log.Error("No terrain found in biome " + biome.defName + " for elevation=" + elevation + ", fertility=" + fertility);
            DebugWarnedMissingTerrain = true;
        }

        return TerrainDefOf.Sand;
    }

    private static TerrainDef DefaultElevationTerrain(IntVec3 c, BiomeDef biome, float elevation)
    {
        if (elevation < 0.55) return null;
        if (elevation < 0.61) return biome.Properties().gravelTerrain ?? TerrainDefOf.Gravel;
        return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
    }

    private static float OptimizeVanillaRiverAngleIfNeeded(float fromMethod)
    {
        if (Landform.AnyGeneratingNonLayer && Landform.GeneratingTile != null)
        {
            var riverData = Landform.GeneratingTile.Rivers;
            return riverData.RiverOutflowWidth > 0 ? riverData.RiverOutflowAngle : riverData.RiverInflowAngle;
        }

        return fromMethod;
    }

    private static Vector3 OptimizeVanillaRiverCenterIfNeeded(Vector3 fromMethod, Map map)
    {
        if (Landform.AnyGeneratingNonLayer && Landform.GeneratingTile != null)
        {
            var position = WorldTileUtils.RiverPositionForTile(Landform.GeneratingTile, 0);
            return new Vector3(position.x * map.Size.x, 0f, position.z * map.Size.z);
        }

        return fromMethod;
    }

    private static void ApplyWaterFlow(Map map)
    {
        if (MapPreviewAPI.IsGeneratingPreview) return;

        var flowFuncAlpha = Landform.GetFeatureScaled(l => l.OutputWaterFlow?.GetFlowAlpha());
        var flowFuncBeta = Landform.GetFeatureScaled(l => l.OutputWaterFlow?.GetFlowBeta());

        if (flowFuncAlpha == null || flowFuncBeta == null) return;

        var waterInfo = map.waterInfo;
        if (waterInfo == null) return;

        const int border = WaterInfo.RiverOffsetMapBorder;

        var bounds = new CellRect(-border, -border, map.Size.x + border * 2, map.Size.z + border * 2);

        var bytes = waterInfo.riverOffsetMap;
        var offsets = new float[bounds.Area * 2];

        if (bytes != null) Buffer.BlockCopy(bytes, 0, offsets, 0, bytes.Length);
        else bytes = new byte[bounds.Area * 8];

        int idx = 0;

        for (int z = bounds.minZ; z <= bounds.maxZ; ++z)
        {
            for (int x = bounds.minX; x <= bounds.maxX; ++x)
            {
                if (RiverFunction == null && (offsets[idx] != 0f || offsets[idx + 1] != 0f)) continue;

                var alpha = (float) flowFuncAlpha.ValueAt(x, z);
                var beta = (float) flowFuncBeta.ValueAt(x, z);

                offsets[idx] = beta;
                offsets[idx + 1] = alpha;
                idx += 2;
            }
        }

        Buffer.BlockCopy(offsets, 0, bytes, 0, offsets.Length * 4);

        waterInfo.riverOffsetMap = bytes;
        waterInfo.GenerateRiverFlowMap();
    }
}

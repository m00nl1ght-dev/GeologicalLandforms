using System;
using System.Collections.Generic;
using GeologicalLandforms.Patches;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public static class BiomeTransition
{
    // [TweakValue("Geological Landforms")]
    public static bool DebugBiomeTransitions;

    // [TweakValue("Geological Landforms", -1f, 1f)]
    public static float ThresholdForPassCheck = 0f;

    // [TweakValue("Geological Landforms", -1f, 1f)]
    public static float ThresholdForRoot = 0.25f;

    // [TweakValue("Geological Landforms", 0f, 1f)]
    public static float PlantLowDensityPassChance = 0.01f;

    private static HashSet<IntVec3> _tpmProcessed;

    public static bool IsTransition(int tile, int nTile, BiomeDef biome, BiomeDef nBiome, int nbId = -1)
    {
        if (biome == nBiome) return false;

        if (!biome.Properties().AllowsBiomeTransition(nBiome)) return false;
        if (!nBiome.Properties().AllowsBiomeTransition(biome)) return false;

        if (!GeologicalLandformsAPI.UnidirectionalBiomeTransitions()) return true;

        var world = Find.World;
        var landformData = world.LandformData();

        if (landformData != null && landformData.HasBiomeTransitions())
        {
            if (nbId < 0) nbId = world.grid.GetNeighborId(tile, nTile);
            return landformData.GetBiomeTransition(tile, nbId);
        }

        bool rev = tile > nTile;
        int min = rev ? nTile : tile;
        int max = rev ? tile : nTile;

        Rand.PushState(Gen.HashCombineInt(min, max));

        bool flag = Rand.Bool ^ rev;

        Rand.PopState();

        return flag;
    }

    public static void PostProcessBiomeGrid(BiomeGrid biomeGrid, WorldTileInfo tile, IntVec2 mapSize)
    {
        var enabled = GeologicalLandformsAPI.PostProcessBiomeTransitions();

        if (!enabled && !DebugBiomeTransitions) return;

        var tempMap = CreateMinimalMap(tile.TileId, mapSize);
        var floodFiller = new FloodFiller(tempMap);

        _tpmProcessed = DebugBiomeTransitions ? new() : null;
        for (int x = 0; x < mapSize.x; x++)
        for (int z = 0; z < mapSize.z; z++)
        {
            var c = new IntVec3(x, 0, z);
            var cBiome = biomeGrid.BiomeAt(c);

            var cTpm = cBiome.TpmOutputAt(c, tempMap, out var sourceTpm);
            if (cTpm == null || !ShouldPostProcessTpm(sourceTpm, cTpm)) continue;

            c.ForEachAdjacent(mapSize, a =>
            {
                var aBiome = biomeGrid.BiomeAt(a);
                if (aBiome != cBiome)
                {
                    floodFiller.FloodFill(a, p =>
                    {
                        var pBiome = biomeGrid.BiomeAt(p);
                        if (pBiome == cBiome) return false;
                        var cpTpm = sourceTpm.PossibleTerrainAt(p, tempMap);
                        if (cpTpm == null) return false;
                        var ppTpm = pBiome.TpmOutputAt(p, tempMap, out _);
                        return ShouldOverride(sourceTpm, cpTpm, ppTpm);
                    }, p =>
                    {
                        biomeGrid.SetBiome(p, cBiome);
                        _tpmProcessed?.Add(p);
                    });
                }
            });
        }

        if (_tpmProcessed != null)
        {
            GeologicalLandformsAPI.Logger.Debug("TPM postprocessor changed biome of " + _tpmProcessed.Count + " tiles.");
        }

        if (!enabled) biomeGrid.SetBiomes(Patch_RimWorld_GenStep_Terrain.BiomeFunction);
    }

    private static bool ShouldPostProcessTpm(TerrainPatchMaker tpm, TerrainDef output)
    {
        if (tpm.thresholds.Count <= 1) return false;

        foreach (var threshold in tpm.thresholds)
        {
            if (threshold.min >= ThresholdForRoot) return true;
            if (threshold.terrain == output) return false;
        }

        return false;
    }

    private static bool ShouldOverride(TerrainPatchMaker tpm, TerrainDef terrain, TerrainDef other)
    {
        foreach (var threshold in tpm.thresholds)
        {
            if (threshold.min >= ThresholdForPassCheck) return true;
            if (threshold.terrain == terrain) return false;
        }

        if (other == null) return true;
        if (terrain.passability > other.passability) return true;
        if (terrain.passability < other.passability) return false;
        if (terrain.pathCost > other.pathCost) return true;
        if (terrain.pathCost < other.pathCost) return false;
        return false;
    }

    private static void ForEachAdjacent(this IntVec3 c, IntVec2 mapSize, Action<IntVec3> action)
    {
        if (c.x > 0) action(new IntVec3(c.x - 1, c.y, c.z));
        if (c.z > 0) action(new IntVec3(c.x, c.y, c.z - 1));
        if (c.x < mapSize.x - 1) action(new IntVec3(c.x + 1, c.y, c.z));
        if (c.z < mapSize.z - 1) action(new IntVec3(c.x, c.y, c.z + 1));
    }

    private static Map CreateMinimalMap(int tile, IntVec2 mapSize)
    {
        var map = new Map { info = { parent = new MapParent { Tile = tile }, Size = new IntVec3(mapSize.x, 1, mapSize.z) } };
        map.cellIndices = new CellIndices(map);
        map.floodFiller = new FloodFiller(map);
        return map;
    }

    public static void DrawDebug(DebugCellDrawer drawer)
    {
        if (!DebugBiomeTransitions) return;
        foreach (var intVec3 in _tpmProcessed)
        {
            drawer.FlashCell(intVec3, 0f, null, 100);
        }
    }

    public static TerrainDef PossibleTerrainAt(this TerrainPatchMaker tpm, IntVec3 c, Map map)
    {
        return tpm.TerrainAt(c, map, tpm.minFertility + 0.5f);
    }

    public static TerrainDef TpmOutputAt(this BiomeDef b, IntVec3 i, Map map, out TerrainPatchMaker tpm)
    {
        foreach (var patchMaker in b.terrainPatchMakers)
        {
            var t = patchMaker.PossibleTerrainAt(i, map);
            if (t != null)
            {
                tpm = patchMaker;
                return t;
            }
        }

        tpm = null;
        return null;
    }
}

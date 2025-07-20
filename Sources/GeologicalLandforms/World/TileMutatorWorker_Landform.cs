#if RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class TileMutatorWorker_Landform : TileMutatorWorker
{
    public Landform Landform;

    public TileMutatorWorker_Landform(TileMutatorDef def) : base(def) {}

    public override string GetLabel(PlanetTile tile)
    {
        if (Landform == null)
            return "";

        if (Landform.IsInternal)
        {
            if (Landform.Id == "BiomeTransitions")
            {
                return "GeologicalLandforms.WorldMap.FeatureLabelBiomeTransition".Translate();
            }

            return "";
        }

        return Landform.TranslatedNameWithDirection(WorldTileInfo.Get(tile).TopologyDirection);
    }

    public override string GetDescription(PlanetTile tile)
    {
        if (Landform == null)
            return "";

        if (!def.description.NullOrEmpty())
            return def.description;

        if (Landform.IsInternal)
        {
            if (Landform.Id == "BiomeTransitions")
            {
                return "GeologicalLandforms.WorldMap.FeatureDescriptionBiomeTransition".Translate();
            }

            return "";
        }

        return "GeologicalLandforms.WorldMap.FeatureDescriptionLandform"
            .Translate(Landform.ModContentPack.ContentSourceLabel().CapitalizeFirst());
    }

    public override void GeneratePostElevationFertility(Map map)
    {
        if (Landform == null || Landform.GeneratingLandforms == null || !Landform.GeneratingLandforms.Contains(Landform)) return;

        var elevationModule = Landform.TransformIntoMapSpace(Landform.OutputElevation?.Get());
        var fertilityModule = Landform.TransformIntoMapSpace(Landform.OutputFertility?.Get());
        var cavesModule = Landform.TransformIntoMapSpace(Landform.OutputCaves?.Get());

        var elevation = MapGenerator.Elevation;
        var fertility = MapGenerator.Fertility;
        var caves = MapGenerator.Caves;

        if (elevationModule != null)
        {
            ApplyBuffered(map.Size, c => (float) elevationModule.ValueAt(c.x, c.z), (c, v) => elevation[c] = v);
        }

        if (fertilityModule != null)
        {
            ApplyBuffered(map.Size, c => (float) fertilityModule.ValueAt(c.x, c.z), (c, v) => fertility[c] = v);
        }

        if (cavesModule != null)
        {
            ApplyBuffered(map.Size, c => elevation[c] > 0.7f ? (float) cavesModule.ValueAt(c.x, c.z) : 0f, (c, v) => caves[c] = v);
        }

        if (map.Biome.Properties().AllowBiomeTransitions)
        {
            var tile = Landform.GeneratingTile;
            var mapSize = Landform.GeneratingMapSize;

            var biomeFunction = Landform.TransformIntoMapSpace(Landform.OutputBiomeGrid?.GetBiomeGrid());

            var biomeGrid = map.BiomeGrid();
            var hasBiomeTransition = false;

            if (tile.HasBorderingBiomes())
            {
                var transition = Landform.OutputBiomeGrid?.ApplyBiomeTransitions(tile, mapSize, biomeFunction);
                if (transition != null)
                {
                    biomeFunction = transition;
                    hasBiomeTransition = true;
                }
            }

            if (biomeGrid != null && biomeFunction != null)
            {
                biomeGrid.Enabled = true;
                biomeGrid.SetBiomes(new GridFunction.Cache<BiomeDef>(map.Size.x, map.Size.z, biomeFunction));

                if (hasBiomeTransition)
                {
                    BiomeTransition.PostProcessBiomeGrid(map);
                }
            }
        }
    }

    public override void GeneratePostTerrain(Map map)
    {
        if (Landform == null || Landform.GeneratingLandforms == null || !Landform.GeneratingLandforms.Contains(Landform)) return;

        var baseFunction = Landform.TransformIntoMapSpace(Landform.OutputTerrain?.GetBase());
        var stoneFunction = Landform.TransformIntoMapSpace(Landform.OutputTerrain?.GetStone());
        var caveFunction = Landform.TransformIntoMapSpace(Landform.OutputTerrain?.GetCave());
        var riverFunction = Landform.TransformIntoMapSpace(Landform.OutputWaterFlow?.GetRiverTerrain());

        if (baseFunction == null && stoneFunction == null && riverFunction == null && caveFunction == null) return;

        TerrainDef TerrainAt(IntVec3 c)
        {
            var biome = map.BiomeAt(c);
            var edifice = c.GetEdifice(map);
            var fertility = MapGenerator.Fertility[c];

            var tRiver = riverFunction?.ValueAt(c.x, c.z);
            if (tRiver == null)
            {
                if (edifice != null && edifice.def.Fillage == FillCategory.Full)
                    return null;

                if (MapGenerator.Caves[c] > 0f)
                    return caveFunction?.ValueAt(c.x, c.z);
            }

            var tBase = baseFunction?.ValueAt(c.x, c.z);
            var tResult = RiverTerrainPriority(tBase, tRiver);

            if (stoneFunction != null && tResult == null)
            {
                tResult = TpmTerrainAt(map, c, biome, fertility);
                tResult ??= stoneFunction.ValueAt(c.x, c.z);
                tResult ??= TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, fertility);
            }

            return tResult;
        }

        void SetTerrain(IntVec3 cell, TerrainDef terrain)
        {
            if (terrain != null)
            {
                map.terrainGrid.SetTerrain(cell, terrain);
            }
        }

        ApplyBuffered(map.Size, TerrainAt, SetTerrain);

        if (!MapPreviewAPI.IsGeneratingPreview)
        {
            ApplyWaterFlow(map);
        }
    }

    private void ApplyWaterFlow(Map map)
    {
        var flowFuncAlpha = Landform.TransformIntoMapSpace(Landform.OutputWaterFlow?.GetFlowAlpha());
        var flowFuncBeta = Landform.TransformIntoMapSpace(Landform.OutputWaterFlow?.GetFlowBeta());

        if (flowFuncAlpha == null || flowFuncBeta == null) return;

        var waterInfo = map.waterInfo;
        if (waterInfo == null) return;

        var bounds = new CellRect(0, 0, map.Size.x, map.Size.z);

        var flowMap = new float[bounds.Area * 2];

        for (int x = 0; x <= bounds.maxX; ++x)
        {
            int xm1 = x == 0 ? x : x - 1;
            int xp1 = x == bounds.maxX ? x : x + 1;
            float xdiv = x == 0 || x == bounds.maxX ? 1f : 2f;

            for (int z = 0; z <= bounds.maxZ; ++z)
            {
                int zm1 = z == 0 ? z : z - 1;
                int zp1 = z == bounds.maxZ ? z : z + 1;
                float zdiv = z == 0 || z == bounds.maxZ ? 1f : 2f;

                var vector3 = new Vector3(
                    (float) (flowFuncAlpha.ValueAt(xp1, z) - flowFuncAlpha.ValueAt(xm1, z)) / xdiv,
                    0f,
                    (float) (flowFuncAlpha.ValueAt(x, zp1) - flowFuncAlpha.ValueAt(x, zm1)) / zdiv
                );

                if (vector3.magnitude > 9.999999747378752E-05)
                {
                    vector3 = vector3.normalized / vector3.magnitude;
                    int index = x * map.Size.z + z;
                    flowMap[index * 2] = vector3.x;
                    flowMap[index * 2 + 1] = vector3.z;
                }
            }
        }

        waterInfo.riverFlowMap = new List<float>(flowMap);
    }

    private TerrainDef TpmTerrainAt(Map map, IntVec3 c, BiomeDef biome, float fertility)
    {
        if (!map.TileInfo.Mutators.Any(m => m.preventPatches))
        {
            bool noPonds = map.TileInfo.Mutators.Any(m => m.preventsPondGeneration);

            foreach (var terrainPatchMaker in biome.terrainPatchMakers)
            {
                if (!noPonds || !terrainPatchMaker.isPond)
                {
                    var terrain = terrainPatchMaker.TerrainAt(c, map, fertility);
                    if (terrain != null) return terrain;
                }
            }
        }

        return null;
    }

    private TerrainDef RiverTerrainPriority(TerrainDef tBase, TerrainDef tRiver)
    {
        if (tRiver == null) return tBase;
        if (tBase == null) return tRiver;
        if (tBase.defName.Contains("Deep")) return tBase;
        if (tRiver.defName.Contains("Deep")) return tRiver;
        if (tBase.IsWater) return tBase;
        if (tRiver.IsWater) return tRiver;
        return tBase;
    }

    private void ApplyBuffered<T>(IntVec3 mapSize, Func<IntVec3, T> generator, Action<IntVec3, T> apply)
    {
        var buffer = new T[mapSize.x, mapSize.z];

        for (int x = 0; x < mapSize.x; x++)
            for (int z = 0; z < mapSize.z; z++)
                buffer[x, z] = generator(new IntVec3(x, 0, z));

        for (int x = 0; x < mapSize.x; x++)
            for (int z = 0; z < mapSize.z; z++)
                apply(new IntVec3(x, 0, z), buffer[x, z]);
    }
}

#endif

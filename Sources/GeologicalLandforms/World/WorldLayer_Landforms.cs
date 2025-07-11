using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld.Planet;
using Unity.Collections;
using UnityEngine;
using Verse;

#if !RW_1_6_OR_GREATER
using System.Collections.Generic;
#endif

namespace GeologicalLandforms;

#if RW_1_6_OR_GREATER
public class WorldLayer_Landforms : WorldDrawLayer
#else
public class WorldLayer_Landforms : WorldLayer
#endif
{
    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate()) yield return obj;

        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) yield break;

        var grid = Find.World.grid;
        int tilesCount = grid.TilesCount;

        var vertData = grid.ExtVertValues();
        var vertOffsets = grid.ExtVertOffsets();

        var anyGraphicInBiomes = BiomeProperties.AnyHasTileGraphic;
        var anyGraphicInBiomeVariants = BiomeVariantDef.AnyHasTileGraphic;
        var anyGraphicInLandforms = LandformManager.AnyHasTileGraphic;

        var failedCount = 0;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            try
            {
                var tile = WorldTileInfo.Get(tileIdx);

                var vertOffset = vertOffsets[tileIdx];
                var vertBound = vertOffsets.IdxBoundFor(vertData, tileIdx);

                var anyVariantGraphic = false;

                if (anyGraphicInBiomes)
                {
                    var atlas = tile.Biome.Properties().worldTileGraphicAtlas;
                    if (atlas != null)
                    {
                        var atlasMat = atlas.DrawMaterial;
                        if (atlasMat != null)
                        {
                            var subMesh = GetSubMesh(atlasMat, out _);
                            atlas.Draw(subMesh, grid, tileIdx, t => t.Biome == tile.Biome, 0.006f);
                        }
                    }
                }

                if (anyGraphicInBiomeVariants && tile.HasBiomeVariants())
                {
                    foreach (var biomeVariant in tile.BiomeVariants)
                    {
                        var material = biomeVariant.DrawMaterial;
                        if (material != null)
                        {
                            var subMesh = GetSubMesh(material, out _);
                            DrawTileSolid(subMesh, vertData, vertOffset, vertBound);
                        }

                        var atlas = biomeVariant.worldTileGraphicAtlas;
                        if (atlas != null)
                        {
                            var atlasMat = atlas.DrawMaterial;
                            if (atlasMat != null)
                            {
                                var subMesh = GetSubMesh(atlasMat, out _);
                                atlas.Draw(subMesh, grid, tileIdx, t => t.HasBiomeVariants() && t.BiomeVariants.Contains(biomeVariant), 0.008f);
                                anyVariantGraphic = true;
                            }
                        }
                    }
                }

                if (anyGraphicInLandforms && !anyVariantGraphic && tile.HasLandforms())
                {
                    foreach (var landform in tile.Landforms)
                    {
                        var atlas = landform.WorldTileGraphic?.Atlas;
                        if (atlas != null)
                        {
                            var atlasMat = atlas.DrawMaterial;
                            if (atlasMat != null)
                            {
                                var subMesh = GetSubMesh(atlasMat, out _);
                                atlas.Draw(subMesh, grid, tileIdx, t => t.HasLandforms() && t.Landforms.Contains(landform), 0.007f);
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                if (failedCount == 0)
                {
                    GeologicalLandformsAPI.Logger.Error("Failed to draw tile graphics for tile " + tileIdx, e);
                }

                failedCount++;
            }
        }

        if (failedCount > 0)
        {
            GeologicalLandformsAPI.Logger.Error("Failed to draw tile graphics for " + failedCount + " world tiles.");
        }

        stopwatch.Stop();

        GeologicalLandformsAPI.Logger.Debug("WorldLayer_Landforms took " + stopwatch.ElapsedMilliseconds + " ms.");

        FinalizeMesh(MeshParts.All);
    }

    #if RW_1_6_OR_GREATER
    private static void DrawTileSolid(LayerSubMesh subMesh, NativeArray<Vector3> vertData, int vertOffset, int vertBound)
    #else
    private static void DrawTileSolid(LayerSubMesh subMesh, List<Vector3> vertData, int vertOffset, int vertBound)
    #endif
    {
        int cVert = 0;
        int count = subMesh.verts.Count;

        for (int vert = vertOffset; vert < vertBound; vert++)
        {
            subMesh.verts.Add(vertData[vert]);

            if (vert < vertBound - 2)
            {
                subMesh.tris.Add(count + cVert + 2);
                subMesh.tris.Add(count + cVert + 1);
                subMesh.tris.Add(count);
            }

            cVert++;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LunarFramework.Utility;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

[HotSwappable]
internal class WorldLayer_Landforms : WorldLayer
{
    private const float HexRadiusFac = 1.1547005f + 0.02f; // 1/(sqrt(3)/2) + extra
    
    private readonly List<int> _tmpNeighbors = new();
    private readonly bool[] _tmpAdjacency = new bool[6];

    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate()) yield return obj;

        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) yield break;

        var grid = Find.World.grid;
        var verts = grid.verts;
        int tilesCount = grid.TilesCount;
        var tileIDToVertsOffsets = grid.tileIDToVerts_offsets;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = WorldTileInfo.Get(tileIdx);

            if (tile.HasBiomeVariants)
            {
                foreach (var biomeVariant in tile.BiomeVariants)
                {
                    var material = biomeVariant.DrawMaterial;

                    if (material != null)
                    {
                        var subMesh = GetSubMesh(material, out _);

                        int cVert = 0;
                        int count = subMesh.verts.Count;
                        int maxVert = tileIdx + 1 < tileIDToVertsOffsets.Count ? tileIDToVertsOffsets[tileIdx + 1] : verts.Count;

                        for (int vert = tileIDToVertsOffsets[tileIdx]; vert < maxVert; vert++)
                        {
                            subMesh.verts.Add(verts[vert]);

                            if (vert < maxVert - 2)
                            {
                                subMesh.tris.Add(count + cVert + 2);
                                subMesh.tris.Add(count + cVert + 1);
                                subMesh.tris.Add(count);
                            }

                            cVert++;
                        }
                    }

                    var graphicAtlas = biomeVariant.worldTileGraphicAtlas;

                    if (graphicAtlas != null)
                    {
                        var atlasMat = graphicAtlas.DrawMaterial;

                        if (atlasMat != null)
                        {
                            var subMesh = GetSubMesh(atlasMat, out _);
                            var tileCenter = grid.GetTileCenter(tileIdx);

                            if (graphicAtlas.strategy == WorldTileGraphicAtlas.StrategyType.Adjacency)
                            {
                                grid.GetTileNeighbors(tileIdx, _tmpNeighbors);

                                if (_tmpNeighbors.Count == 6)
                                {
                                    for (int i = 0; i < _tmpNeighbors.Count; i++)
                                    {
                                        var nbTile = WorldTileInfo.Get(_tmpNeighbors[i]);
                                        _tmpAdjacency[i] = nbTile.HasBiomeVariants && nbTile.BiomeVariants.Contains(biomeVariant);
                                    }

                                    var variant = graphicAtlas.GetVariantForAdjaceny(_tmpAdjacency, out var rotationIdx);
                                    var facing = grid.GetTileCenter(_tmpNeighbors[rotationIdx]);
                                    var size = Vector3.Distance(tileCenter, facing) * HexRadiusFac;

                                    graphicAtlas.Draw(subMesh, tileCenter, size, variant, facing);
                                }
                            }
                            else
                            {
                                graphicAtlas.Draw(subMesh, tileCenter, grid.averageTileSize);
                            }
                        }
                    }
                }
            }
        }

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("WorldLayer_Landforms took " + stopwatch.ElapsedMilliseconds + " ms.");

        FinalizeMesh(MeshParts.All);
    }
}

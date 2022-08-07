using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

internal class WorldLayer_LocalBiomes : WorldLayer
{
    [TweakValue("Geological Landforms", 0, 1)]
    public static float Lerp = 0.5f;
    
    [TweakValue("Geological Landforms", -1, 1)]
    public static float Offset = 0.001f;
    
    private readonly List<int> _tmpNeighbors = new();
    
    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate()) yield return obj;
        
        if (!BiomeTransition.UnidirectionalTransitions) yield break;
        
        var grid = Find.World.grid;
        var verts = grid.verts;
        int tilesCount = grid.TilesCount;
        var viewCenter = grid.viewCenter;
        var tileIDToVertsOffsets = grid.tileIDToVerts_offsets;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = grid[tileIdx];
            var biome = tile.biome;
            
            if (!biome.canBuildBase || Main.IsBiomeExcluded(biome)) continue;
            
            grid.GetTileNeighbors(tileIdx, _tmpNeighbors);
            
            for (var i = 0; i < _tmpNeighbors.Count; i++)
            {
                int nIdx = _tmpNeighbors[i];
                var nTile = grid[nIdx];
                var nBiome = nTile.biome;

                bool isWater = nBiome == BiomeDefOf.Ocean || nBiome == BiomeDefOf.Lake;
                if (isWater || BiomeTransition.IsTransition(tileIdx, nIdx, biome, nBiome))
                {
                    var subMesh = GetSubMesh(nTile.biome.DrawMaterial);
                    int existingVertCount = subMesh.verts.Count;

                    var tileCenter = grid.GetTileCenter(tileIdx);
                    var vertsOffset = tileIDToVertsOffsets[tileIdx];
                    
                    var elev = new Vector3(isWater ? 0 : Math.Max(tile.elevation, 0), 0, 0);
                    var nElev = new Vector3(isWater ? 0 : Math.Max(nTile.elevation, 0), 0, 0);

                    Vector3 ApplyOffset(Vector3 vec) => vec + (vec - viewCenter).normalized * Offset;
                    
                    var vert1 = ApplyOffset(verts[vertsOffset + i]);
                    var vert2 = ApplyOffset(verts[vertsOffset + (i + 1) % 6]);
                    var vert3 = ApplyOffset(Vector3.LerpUnclamped(vert2, tileCenter, Lerp));
                    var vert4 = ApplyOffset(Vector3.LerpUnclamped(vert1, tileCenter, Lerp));
                    
                    subMesh.verts.Add(vert1);
                    subMesh.verts.Add(vert2);
                    subMesh.verts.Add(vert3);
                    subMesh.verts.Add(vert4);
                    
                    subMesh.tris.Add(existingVertCount + 2);
                    subMesh.tris.Add(existingVertCount + 1);
                    subMesh.tris.Add(existingVertCount);
                    
                    subMesh.tris.Add(existingVertCount + 3);
                    subMesh.tris.Add(existingVertCount + 2);
                    subMesh.tris.Add(existingVertCount);
                    
                    subMesh.uvs.Add(nElev);
                    subMesh.uvs.Add(nElev);

                    subMesh.uvs.Add(elev);
                    subMesh.uvs.Add(elev);
                }
            }
        }
        
        stopwatch.Stop();
        Log.Message("took " + stopwatch.ElapsedMilliseconds + " ms");

        FinalizeMesh(MeshParts.All);
    }
}
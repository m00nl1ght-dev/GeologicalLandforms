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
    private readonly List<Vector3> _elevationValues = new();
    
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
        
        //foreach (object interpolatedVerticesParam in CalculateInterpolatedVerticesParams())
        //    yield return interpolatedVerticesParam;

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = grid[tileIdx];
            var biome = tile.biome;
            
            if (BiomeUtils.IsBiomeExcluded(biome)) continue;
            
            grid.GetTileNeighbors(tileIdx, _tmpNeighbors);
            
            for (var i = 0; i < _tmpNeighbors.Count; i++)
            {
                int nIdx = _tmpNeighbors[i];
                var nTile = grid[nIdx];
                var nBiome = nTile.biome;

                bool isWater = nBiome == BiomeDefOf.Ocean || nBiome == BiomeDefOf.Lake;
                if (BiomeTransition.IsTransition(tileIdx, nIdx, biome, nBiome, false))
                {
                    var subMesh = GetSubMesh(nTile.biome.DrawMaterial);
                    int existingVertCount = subMesh.verts.Count;

                    var tileCenter = grid.GetTileCenter(tileIdx);
                    var vertsOffset = tileIDToVertsOffsets[tileIdx];
                    
                    var elev = isWater ? Math.Min(tile.elevation, 0) : Math.Max(tile.elevation, 0);
                    var nElev = isWater ? Math.Min(tile.elevation, 0) : Math.Max(nTile.elevation, 0);

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
                    
                    subMesh.uvs.Add(new Vector3(nElev, 0, 0));
                    subMesh.uvs.Add(new Vector3(nElev, 0, 0));
                    
                    subMesh.uvs.Add(new Vector3(elev, 0, 0));
                    subMesh.uvs.Add(new Vector3(elev, 0, 0));

                    /*
                    subMesh.uvs.Add(_elevationValues[vertsOffset + i]);
                    subMesh.uvs.Add(_elevationValues[vertsOffset + (i + 1) % 6]);
                    subMesh.uvs.Add(_elevationValues[vertsOffset + (i + 1) % 6]);
                    subMesh.uvs.Add(_elevationValues[vertsOffset + i]);
                    */
                }
            }
        }
        
        stopwatch.Stop();
        Log.Message("WorldLayer_LocalBiomes took " + stopwatch.ElapsedMilliseconds + " ms.");

        FinalizeMesh(MeshParts.All);
        //_elevationValues.Clear();
        //_elevationValues.TrimExcess();
    }

    private IEnumerable CalculateInterpolatedVerticesParams()
    {
        _elevationValues.Clear();
        var grid = Find.World.grid;
        int tilesCount = grid.TilesCount;
        var verts = grid.verts;
        var tileIDToVerts_offsets = grid.tileIDToVerts_offsets;
        var tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
        var tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
        var tiles = grid.tiles;
        for (int i = 0; i < tilesCount; ++i)
        {
            var tile1 = tiles[i];
            float elevation = tile1.elevation;
            int num1 = i + 1 < tileIDToNeighbors_offsets.Count
                ? tileIDToNeighbors_offsets[i + 1]
                : tileIDToNeighbors_values.Count;
            int num2 = i + 1 < tilesCount ? tileIDToVerts_offsets[i + 1] : verts.Count;
            for (int index1 = tileIDToVerts_offsets[i]; index1 < num2; ++index1)
            {
                var vector3 = new Vector3();
                vector3.x = elevation;
                bool flag = false;
                for (int index2 = tileIDToNeighbors_offsets[i]; index2 < num1; ++index2)
                {
                    int num3 = tileIDToNeighbors_values[index2] + 1 < tileIDToVerts_offsets.Count
                        ? tileIDToVerts_offsets[tileIDToNeighbors_values[index2] + 1]
                        : verts.Count;
                    for (int index3 = tileIDToVerts_offsets[tileIDToNeighbors_values[index2]]; index3 < num3; ++index3)
                    {
                        if (verts[index3] == verts[index1])
                        {
                            var tile2 = tiles[tileIDToNeighbors_values[index2]];
                            if (!flag)
                            {
                                if (tile2.elevation >= 0.0 && elevation <= 0.0 ||
                                    tile2.elevation <= 0.0 && elevation >= 0.0)
                                {
                                    flag = true;
                                    break;
                                }

                                if (tile2.elevation > (double)vector3.x)
                                {
                                    vector3.x = tile2.elevation;
                                }
                            }

                            break;
                        }
                    }
                }

                if (flag)
                    vector3.x = 0.0f;
                if (tile1.biome.DrawMaterial.shader != ShaderDatabase.WorldOcean && vector3.x < 0.0)
                    vector3.x = 0.0f;
                _elevationValues.Add(vector3);
            }

            if (i % 1000 == 0)
                yield return null;
        }
    }
}
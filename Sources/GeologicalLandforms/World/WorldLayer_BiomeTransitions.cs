using System;
using System.Collections;
using System.Diagnostics;
using LunarFramework.Utility;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

#if RW_1_6_OR_GREATER
public class WorldLayer_BiomeTransitions : WorldDrawLayer
#else
public class WorldLayer_BiomeTransitions : WorldLayer
#endif
{
    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate()) yield return obj;

        var world = Find.World;

        if (!BiomeTransition.UnidirectionalBiomeTransitions.Apply(world)) yield break;
        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) yield break;

        var grid = world.grid;
        int tilesCount = grid.TilesCount;

        var vertData = grid.ExtVertValues();
        var vertOffsets = grid.ExtVertOffsets();

        var nbData = grid.ExtNbValues();
        var nbOffsets = grid.ExtNbOffsets();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int tileIdx = 0; tileIdx < tilesCount; ++tileIdx)
        {
            var tile = grid[tileIdx];
            var biome = tile.biome;

            if (!biome.Properties().allowBiomeTransitions) continue;

            var nbOffset = nbOffsets[tileIdx];
            if (nbOffsets.IdxBoundFor(nbData, tileIdx) - nbOffset != 6) continue;

            for (var i = 0; i < 6; i++)
            {
                int nIdx = nbData[nbOffset + i];
                var nTile = grid[nIdx];
                var nBiome = nTile.biome;

                if (BiomeTransition.IsTransition(tileIdx, nIdx, biome, nBiome, i))
                {
                    var subMesh = GetSubMesh(nBiome.DrawMaterial);
                    int existingVertCount = subMesh.verts.Count;

                    var tileCenter = grid.GetTileCenter(tileIdx);
                    var vertOffset = vertOffsets[tileIdx];
                    bool isWater = nBiome.Properties().isWaterCovered;

                    var elev = isWater ? Math.Min(tile.elevation, 0) : Math.Max(tile.elevation, 0);
                    var nElev = isWater ? Math.Min(tile.elevation, 0) : Math.Max(nTile.elevation, 0);

                    Vector3 ApplyOffset(Vector3 vec) => vec + vec.normalized * 0.001f;

                    var vert1 = ApplyOffset(vertData[vertOffset + i]);
                    var vert2 = ApplyOffset(vertData[vertOffset + (i + 1) % 6]);
                    var vert3 = ApplyOffset(Vector3.LerpUnclamped(vert2, tileCenter, 0.5f));
                    var vert4 = ApplyOffset(Vector3.LerpUnclamped(vert1, tileCenter, 0.5f));

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
                }
            }
        }

        stopwatch.Stop();
        GeologicalLandformsAPI.Logger.Debug("WorldLayer_BiomeTransitions took " + stopwatch.ElapsedMilliseconds + " ms.");

        FinalizeMesh(MeshParts.All);
    }
}

using System.Collections;
using System.Diagnostics;
using System.Linq;
using LunarFramework.Utility;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

[HotSwappable]
internal class WorldLayer_Landforms : WorldLayer
{
    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate()) yield return obj;

        if (!GeologicalLandformsAPI.LunarAPI.IsInitialized()) yield break;

        var grid = Find.World.grid;
        int tilesCount = grid.TilesCount;

        var vertData = grid.verts;
        var vertOffsets = grid.tileIDToVerts_offsets;

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

                        var vertOffset = vertOffsets[tileIdx];
                        int vertBound = vertOffsets.IdxBoundFor(vertData, tileIdx);

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

                    var graphicAtlas = biomeVariant.worldTileGraphicAtlas;

                    if (graphicAtlas != null)
                    {
                        var atlasMat = graphicAtlas.DrawMaterial;

                        if (atlasMat != null)
                        {
                            var subMesh = GetSubMesh(atlasMat, out _);
                            graphicAtlas.Draw(subMesh, grid, tileIdx, t => t.HasBiomeVariants && t.BiomeVariants.Contains(biomeVariant));
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

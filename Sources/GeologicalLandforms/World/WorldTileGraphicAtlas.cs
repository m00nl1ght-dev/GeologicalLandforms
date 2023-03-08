using System;
using LunarFramework.Utility;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class WorldTileGraphicAtlas
{
    [NoTranslate]
    public string texture;

    public float drawSize = 1f;

    public float altitude = -1f;

    public int renderQueue = 3515;

    public IntVec2 atlasSize = new(4, 4);

    public DrawMode drawMode = DrawMode.HexRandom;

    public int VariantCount => atlasSize.x * atlasSize.z;

    public IntVec2 AtlasCoords(int idx) => new(idx % atlasSize.x, idx / atlasSize.x);

    [Unsaved]
    private Material _cachedMat;

    public Material DrawMaterial
    {
        get
        {
            if (_cachedMat != null) return _cachedMat;
            if (texture.NullOrEmpty()) return null;
            _cachedMat = MaterialPool.MatFrom(texture, ShaderDatabase.WorldOverlayTransparentLit, renderQueue);
            return _cachedMat;
        }
    }

    public void Refresh()
    {
        _cachedMat = null;
    }

    private static readonly bool[] _tmpAdjacency = new bool[6];

    public void Draw(LayerSubMesh mesh, WorldGrid grid, int tile, Predicate<WorldTileInfo> adjTest = null, float defaultAlt = 0.002f)
    {
        var alt = altitude >= 0 ? altitude : defaultAlt;

        switch (drawMode)
        {
            case DrawMode.HexRandom:
                DrawHex(mesh, grid, tile, alt, null);
                break;
            case DrawMode.HexAdjacency:
                DrawHex(mesh, grid, tile, alt, adjTest);
                break;
            case DrawMode.HexAdjacencyCaves:
                DrawHex(mesh, grid, tile, alt, t => t.DepthInCaveSystem > 0 && t.Hilliness == Hilliness.Impassable);
                break;
            case DrawMode.QuadRandom:
                DrawQuad(mesh, grid, tile, alt, true);
                break;
            case DrawMode.QuadStatic:
                DrawQuad(mesh, grid, tile, alt, false);
                break;
        }
    }

    private void DrawQuad(LayerSubMesh mesh, WorldGrid grid, int tile, float alt, bool randRotation)
    {
        var tileCenter = grid.GetTileCenter(tile);
        var atlasCoords = AtlasCoords(Rand.Range(0, VariantCount));
        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter, grid.averageTileSize * drawSize, alt, mesh, false, randRotation, false);
        WorldRendererUtility.PrintTextureAtlasUVs(atlasCoords.x, atlasCoords.z, atlasSize.x, atlasSize.z, mesh);
    }

    private void DrawHex(LayerSubMesh mesh, WorldGrid grid, int tile, float alt, Predicate<WorldTileInfo> adjTest)
    {
        var vertData = grid.verts;
        var vertOffsets = grid.tileIDToVerts_offsets;
        var vertOffset = vertOffsets[tile];

        var nbData = grid.tileIDToNeighbors_values;
        var nbOffsets = grid.tileIDToNeighbors_offsets;
        var nbOffset = nbOffsets[tile];

        if (nbOffsets.IdxBoundFor(nbData, tile) - nbOffset != 6) return;

        int rotationIdx = -1;

        var variantIdx = adjTest != null ? CalculateAdjacency(grid, tile, adjTest, out rotationIdx) : Rand.Range(0, VariantCount);
        var rotOffset = rotationIdx < 0 ? Rand.Range(0, 6) : FindFirstSharedVert(grid, tile, nbData[nbOffset + rotationIdx]);

        var atlasCoords = AtlasCoords(variantIdx);
        var vertCount = mesh.verts.Count;

        for (var i = 0; i < 6; i++)
        {
            var idx = (i + rotOffset) % 6;
            var vert = vertData[vertOffset + idx];

            mesh.verts.Add(vert + vert.normalized * alt);

            var uvHex = ExtensionUtils.HexTextureVert(i);
            mesh.uvs.Add(new Vector3((atlasCoords.x + uvHex.x) / atlasSize.x, (atlasCoords.z + uvHex.y) / atlasSize.z));

            if (i < 4)
            {
                mesh.tris.Add(vertCount + i + 2);
                mesh.tris.Add(vertCount + i + 1);
                mesh.tris.Add(vertCount);
            }
        }
    }

    private int FindFirstSharedVert(WorldGrid grid, int tile, int nbTile)
    {
        var vertData = grid.verts;
        var vertOffsets = grid.tileIDToVerts_offsets;
        var vertOffset = vertOffsets[tile];
        var nbVertOffset = vertOffsets[nbTile];
        var nbVertBound = vertOffsets.IdxBoundFor(vertData, nbTile);

        bool Check(int i)
        {
            var vert = vertData[vertOffset + i];
            for (int j = nbVertOffset; j < nbVertBound; j++)
                if (vertData[j] == vert)
                    return true;
            return false;
        }

        if (Check(5)) return Check(0) ? 1 : 0;
        for (int i = 0; i < 4; i++)
            if (Check(i))
                return i + 2;

        GeologicalLandformsAPI.Logger.Debug("Failed to find shared vert between neighbors!");
        return 0;
    }

    private int CalculateAdjacency(WorldGrid grid, int tile, Predicate<WorldTileInfo> adjTest, out int rotationIdx)
    {
        var adjFirst = -1;
        var adjLast = -1;
        var adjCount = 0;
        var sepMax = 0;

        rotationIdx = -1;

        var nbData = grid.tileIDToNeighbors_values;
        var nbOffset = grid.tileIDToNeighbors_offsets[tile];

        for (int i = 0; i < 6; i++)
        {
            var nbTile = WorldTileInfo.Get(nbData[nbOffset + i]);
            if (adjTest(nbTile))
            {
                if (adjCount == 0) adjFirst = i;
                else
                {
                    var sep = i - adjLast - 1;
                    if (sep > sepMax || (sep == sepMax && Rand.Bool))
                    {
                        rotationIdx = i;
                        sepMax = sep;
                    }
                }

                _tmpAdjacency[i] = true;

                adjLast = i;
                adjCount++;
            }
            else
            {
                _tmpAdjacency[i] = false;
            }
        }

        if (adjCount == 6) return 13;

        if (adjCount == 0) return 0;

        if (adjCount == 1)
        {
            rotationIdx = adjFirst;
            return 1;
        }

        var sepLeap = 5 - adjLast + adjFirst;
        if (sepLeap > sepMax || (sepLeap == sepMax && Rand.Bool))
        {
            rotationIdx = adjFirst;
            sepMax = sepLeap;
        }

        if (adjCount == 2)
        {
            if (sepMax == 4) return 2;
            if (sepMax == 3) return 3;
            return 4;
        }

        if (adjCount == 3)
        {
            if (sepMax == 3) return 5;
            if (sepMax == 1) return 8;
            if (_tmpAdjacency[(rotationIdx + 1) % 6]) return 6;
            return 7;
        }

        if (adjCount == 4)
        {
            if (sepMax == 2) return 9;
            if (!_tmpAdjacency[(rotationIdx + 2) % 6]) return 11;
            if (!_tmpAdjacency[(rotationIdx + 1) % 6]) rotationIdx = (rotationIdx + 2) % 6;
            return 10;
        }

        return 12;
    }

    public enum DrawMode
    {
        HexRandom,
        HexAdjacency,
        HexAdjacencyCaves,
        QuadRandom,
        QuadStatic
    }
}

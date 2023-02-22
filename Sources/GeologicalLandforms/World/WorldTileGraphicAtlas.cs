using System.Collections.Generic;
using LunarFramework.Utility;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

[HotSwappable]
public class WorldTileGraphicAtlas
{
    [NoTranslate]
    public string texture;

    public float drawSize = 1f;

    public int renderQueue = 3505;

    public IntVec2 atlasSize = new(4, 4);

    public StrategyType strategy = StrategyType.Random;

    public DrawMode drawMode = DrawMode.Hex;

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

    private const float HexRadiusFac = 1.1547005f; // 1/(sqrt(3)/2)

    private static readonly List<int> _tmpNeighbors = new();
    private static readonly bool[] _tmpAdjacency = new bool[6];

    public int VariantCount => atlasSize.x * atlasSize.z;

    public IntVec2 AtlasCoordsForVariant(int idx) => new(idx / atlasSize.x, idx % atlasSize.x);

    public void Draw(LayerSubMesh mesh, Vector3 tileCenter, float tileSize, int variant = -1, Vector3? facing = null, float alt = 0.002f)
    {
        if (variant < 0) variant = Rand.Range(0, VariantCount);
        var coords = AtlasCoordsForVariant(variant);

        if (facing.HasValue)
        {
            PrintQuadFacing(tileCenter, facing.Value, tileSize * drawSize, alt, mesh);
        }
        else
        {
            WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter, tileSize * drawSize, alt, mesh, false, false, false);
        }

        WorldRendererUtility.PrintTextureAtlasUVs(coords.x, coords.z, atlasSize.x, atlasSize.z, mesh);
    }

    public static void PrintQuadFacing(Vector3 pos, Vector3 facing, float size, float altOffset, LayerSubMesh subMesh)
    {
        WorldRendererUtility.GetTangentialVectorFacing(pos, facing, out var second, out var first);

        var half = size * 0.5f;
        var normalized = pos.normalized;
        int count = subMesh.verts.Count;

        var vec1 = pos - first * half - second * half + normalized * altOffset;
        var vec2 = pos - first * half + second * half + normalized * altOffset;
        var vec3 = pos + first * half + second * half + normalized * altOffset;
        var vec4 = pos + first * half - second * half + normalized * altOffset;

        subMesh.verts.Add(vec1);
        subMesh.verts.Add(vec2);
        subMesh.verts.Add(vec3);
        subMesh.verts.Add(vec4);

        subMesh.tris.Add(count);
        subMesh.tris.Add(count + 1);
        subMesh.tris.Add(count + 2);
        subMesh.tris.Add(count);
        subMesh.tris.Add(count + 2);
        subMesh.tris.Add(count + 3);
    }

    public int GetVariantForAdjaceny(bool[] adjacency, out int rotationIdx)
    {
        rotationIdx = 0;
        return 0; // TODO
    }

    public enum StrategyType
    {
        Random,
        RandomUpright,
        Adjacency
    }

    public enum DrawMode
    {
        Hex,
        Quad
    }
}

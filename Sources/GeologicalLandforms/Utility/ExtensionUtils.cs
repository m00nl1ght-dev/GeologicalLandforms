using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using RimWorld;
using RimWorld.Planet;
using TerrainGraph;
using Unity.Collections;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms;

public static class ExtensionUtils
{
    private static LandformData _landformDataCache;

    public static LandformData LandformData(this World world)
    {
        if (_landformDataCache?.world == world) return _landformDataCache;
        if (world == null) return null;
        _landformDataCache = world.GetComponent<LandformData>();
        return _landformDataCache;
    }

    private static BiomeGrid _biomeGridCache;

    public static BiomeGrid BiomeGrid(this Map map)
    {
        if (_biomeGridCache?.map == map) return _biomeGridCache;
        if (map == null) return null;
        _biomeGridCache = map.GetComponent<BiomeGrid>();
        return _biomeGridCache;
    }

    private static Vector2[] _hexTextureVertCache;

    public static Vector2 HexTextureVert(int idx)
    {
        if (_hexTextureVertCache == null)
        {
            _hexTextureVertCache = new Vector2[6];

            for (int i = 0; i < 6; i++)
            {
                _hexTextureVertCache[i] = (GenGeo.RegularPolygonVertexPosition(6, i) + Vector2.one) * 0.5f;
            }
        }

        return _hexTextureVertCache[idx];
    }

    public static void ClearCaches()
    {
        _landformDataCache = null;
        _biomeGridCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MinXZ(this IntVec3 vec) => Math.Min(vec.x, vec.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect Moved(this Rect rect, float x, float y) => new(rect.position + new Vector2(x, y), rect.size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(this T[,] grid, IntVec3 c) => grid[c.x, c.z];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<T>(this T[,] grid, IntVec3 c, T value) => grid[c.x, c.z] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ValueAt<T>(this IGridFunction<T> func, IntVec3 pos) => func.ValueAt(pos.x, pos.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BiomeProperties Properties(this BiomeDef biomeDef) => BiomeProperties.Get(biomeDef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasStableCaveRoofs(this Map map) => map.TileInfo.hilliness == Hilliness.Impassable || map.Biome.Properties().hasStableCaveRoofs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IdxBoundFor(this List<int> offsets, IList values, int tileIdx)
        => tileIdx + 1 < offsets.Count ? offsets[tileIdx + 1] : values.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IdxBoundFor<T>(this NativeArray<int> offsets, NativeArray<T> values, int tileIdx) where T : struct
        => tileIdx + 1 < offsets.Length ? offsets[tileIdx + 1] : values.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MurmurCombine(this int value, int other) => MurmurHash.GetInt((uint) value, (uint) other);

    public static bool HasFinishedGenerating(this World world)
        => world != null && Patch_RimWorld_World.LastFinalizedWorldRef?.Target == world;

    public static bool HasLandforms(this IWorldTileInfo tile)
        => tile.Landforms?.Count > 0;

    public static bool HasLandform(this IWorldTileInfo tile, string landformId)
        => tile.Landforms != null && tile.Landforms.Any(l => l.Id == landformId);

    public static bool IsTopologyCompatible(this IWorldTileInfo tile, Topology topology)
        => topology.IsCompatible(tile.Topology, false);

    public static bool HasCoast(this IWorldTileInfo tile, CoastType coast)
        => tile.Coast.Any(c => c == coast);

    public static bool HasBiome(this IWorldTileInfo tile, string defName)
        => tile.Biome != null && tile.Biome.defName == defName;

    public static bool HasLandform(this IWorldTileInfo tile, Landform landform)
        => tile.Landforms != null && tile.Landforms.Contains(landform);

    public static bool HasWorldObject(this IWorldTileInfo tile, string defName)
        => tile.WorldObject != null && tile.WorldObject.def.defName == defName;

    public static bool HasBorderingBiomes(this IWorldTileInfo tile)
        => tile.BorderingBiomes?.Count > 0;

    public static int BorderingBiomesCount(this IWorldTileInfo tile)
        => tile.BorderingBiomes?.Count ?? 0;

    public static bool HasBiomeVariants(this IWorldTileInfo tile)
        => tile.BiomeVariants?.Count > 0;

    #if RW_1_6_OR_GREATER

    public static SurfaceTile.RiverLink LargestRiverLink(this SurfaceTile tile)
        => tile.Rivers?.OrderByDescending(r => r.river.WidthOnWorld()).FirstOrDefault() ?? default;

    public static SurfaceTile.RoadLink LargestRoadLink(this SurfaceTile tile)
        => tile.Roads?.OrderByDescending(r => r.road.WidthOnWorld()).FirstOrDefault() ?? default;

    #else

    public static Tile.RiverLink LargestRiverLink(this Tile tile)
        => tile.Rivers?.OrderByDescending(r => r.river.WidthOnWorld()).FirstOrDefault() ?? default;

    public static Tile.RoadLink LargestRoadLink(this Tile tile)
        => tile.Roads?.OrderByDescending(r => r.road.WidthOnWorld()).FirstOrDefault() ?? default;

    #endif

    public static float WidthOnWorld(this RiverDef riverDef)
        => riverDef?.widthOnWorld ?? 0f;

    public static float WidthOnWorld(this RoadDef roadDef)
        => roadDef?.worldRenderSteps is { Count: > 0 } ? roadDef.worldRenderSteps[0].width : 0f;

    public static bool HasTerrain(this ICtxMapCell ctx, string defName)
        => ctx.Map.terrainGrid.TerrainAt(ctx.MapCell)?.defName == defName;

    public static bool HasTerrainTag(this ICtxMapCell ctx, string tag)
        => ctx.Map.terrainGrid.TerrainAt(ctx.MapCell)?.tags?.Contains(tag) ?? false;

    public static bool HasRoof(this ICtxMapCell ctx, string defName)
        => (ctx.Map.roofGrid.RoofAt(ctx.MapCell)?.defName ?? "Unroofed") == defName;

    public static float SmoothDist(this FloatRange range, FloatRange bounds, float value, float smooth = 0.1f)
    {
        if (!range.Includes(value)) return 0f;

        var s = Mathf.Clamp01(smooth);
        var t = Mathf.InverseLerp(range.min, range.max, value);

        if (s > 0.5f && range.min > bounds.min && range.max < bounds.max) s = 0.5f;

        if (range.min > bounds.min && t < s) return t / s;
        if (range.max < bounds.max && t > 1f - s) return (1f - t) / s;

        return 1f;
    }

    public static string ContentSourceLabel(this ModContentPack mcp)
    {
        if (mcp == null) return "unknown";
        if (mcp.IsCoreMod) return "vanilla";
        if (mcp.IsOfficialMod) return mcp.Name + " DLC";
        return mcp.Name;
    }

    public static ModuleBase AsModule(this IGridFunction<double> gridFunction)
        => new ModuleAdapter(gridFunction);

    private class ModuleAdapter : ModuleBase
    {
        private readonly IGridFunction<double> _inner;

        public ModuleAdapter(IGridFunction<double> inner) : base(0)
        {
            _inner = inner;
        }

        public override double GetValue(double x, double y, double z)
        {
            return _inner.ValueAt(x, z);
        }
    }
}

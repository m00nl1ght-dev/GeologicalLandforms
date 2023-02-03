using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public static class ExtensionUtils
{
    private static LandformData _landformDataCache;

    public static LandformData LandformData(this World world)
    {
        if (world == null) return null;
        if (_landformDataCache?.world == world) return _landformDataCache;
        _landformDataCache = world.GetComponent<LandformData>();
        return _landformDataCache;
    }

    private static BiomeGrid _biomeGridCache;

    public static BiomeGrid BiomeGrid(this Map map)
    {
        if (map == null) return null;
        if (_biomeGridCache?.map == map) return _biomeGridCache;
        _biomeGridCache = map.GetComponent<BiomeGrid>();
        return _biomeGridCache;
    }

    public static void ClearCaches()
    {
        _landformDataCache = null;
        _biomeGridCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MinXZ(this IntVec3 vec) => Math.Min(vec.x, vec.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(this T[,] grid, IntVec3 c) => grid[c.x, c.z];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<T>(this T[,] grid, IntVec3 c, T value) => grid[c.x, c.z] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BiomeProperties Properties(this BiomeDef biomeDef) => BiomeProperties.Get(biomeDef);

    public static bool HasLandform(this IWorldTileInfo tile, string landformId)
        => tile.Landforms != null && tile.Landforms.Any(l => l.Id == landformId);

    public static bool IsTopologyCompatible(this IWorldTileInfo tile, Topology topology)
        => topology.IsCompatible(tile.Topology, false);

    public static bool HasCoast(this IWorldTileInfo tile, IWorldTileInfo.CoastType coast)
        => tile.Coast.Any(c => c == coast);

    public static bool HasBiome(this IWorldTileInfo tile, string defName)
        => tile.Biome != null && tile.Biome.defName == defName;
    
    public static bool HasWorldObject(this IWorldTileInfo tile, string defName)
        => tile.WorldObject != null && tile.WorldObject.def.defName == defName;

    public static int BorderingBiomesCount(this IWorldTileInfo tile)
        => tile.BorderingBiomes?.Count ?? 0;
    
    public static float MainRiverSize(this IWorldTileInfo tile)
        => tile.MainRiver?.widthOnWorld ?? 0f;
    
    public static float MainRoadSize(this IWorldTileInfo tile)
        => 1f - (tile.MainRoad?.movementCostMultiplier ?? 1f);
    
    public static bool HasTerrain(this XmlContext ctx, string defName)
        => ctx.Map.terrainGrid.TerrainAt(ctx.MapCell)?.defName == defName;
    
    public static bool HasTerrainTag(this XmlContext ctx, string tag)
        => ctx.Map.terrainGrid.TerrainAt(ctx.MapCell)?.tags?.Contains(tag) ?? false;
    
    public static bool HasRoof(this XmlContext ctx, string defName)
        => (ctx.Map.roofGrid.RoofAt(ctx.MapCell)?.defName ?? "Unroofed") == defName;
}

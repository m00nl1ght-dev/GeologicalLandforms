using System;
using System.Xml;
using GeologicalLandforms.Patches;
using LunarFramework.Utility;
using LunarFramework.XML;
using UnityEngine;
using Verse;
using Verse.Noise;
using static LunarFramework.XML.XmlDynamicValueSupport;
using static GeologicalLandforms.IWorldTileInfo;

namespace GeologicalLandforms;

public static class XmlDynamicValueSetup
{
    static XmlDynamicValueSetup()
    {
        // ### Numeric suppliers for early world tile context ###

        var earlyNumSup = XmlDynamicValue<float, ICtxEarlyTile>.SupplierSpecs;

        earlyNumSup.Register("hilliness", ctx => (float) ctx.Tile.hilliness);
        earlyNumSup.Register("elevation", ctx => ctx.Tile.elevation);
        earlyNumSup.Register("temperature", ctx => ctx.Tile.temperature);
        earlyNumSup.Register("rainfall", ctx => ctx.Tile.rainfall);
        earlyNumSup.Register("swampiness", ctx => ctx.Tile.swampiness);
        earlyNumSup.Register("pollution", ctx => ctx.Tile.pollution);
        earlyNumSup.Register("longitude", ctx => ctx.World.grid.LongLatOf(ctx.TileId).x);
        earlyNumSup.Register("latitude", ctx => ctx.World.grid.LongLatOf(ctx.TileId).y);

        earlyNumSup.Register("perlinWorld", PerlinNoiseInWorld);
        earlyNumSup.Register("randomValueWorld", RandomValueInWorld);
        earlyNumSup.Register("depthInCaveSystem", GetCaveSystemDepthAt);

        // ### Numeric suppliers for full world tile context ###

        var tileNumSup = XmlDynamicValue<float, ICtxTile>.SupplierSpecs;

        tileNumSup.Register("borderingBiomes", ctx => ctx.TileInfo.BorderingBiomesCount());
        tileNumSup.Register("river", ctx => ctx.TileInfo.MainRiverSize());
        tileNumSup.Register("road", ctx => ctx.TileInfo.MainRoadSize());
        tileNumSup.Register("topologyValue", ctx => ctx.TileInfo.TopologyValue);
        tileNumSup.Register("depthInCaveSystem", ctx => ctx.TileInfo.DepthInCaveSystem);

        tileNumSup.InheritFrom(earlyNumSup);

        // ### Boolean suppliers for full world tile context ###

        var tileBoolSup = XmlDynamicValue<bool, ICtxTile>.SupplierSpecs;

        tileBoolSup.Register("landform", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasLandform(str)));
        tileBoolSup.Register("topology", SupplierWithParam<bool, ICtxTile, Topology>((p, ctx) => ctx.TileInfo.IsTopologyCompatible(p)));
        tileBoolSup.Register("coast", SupplierWithParam<bool, ICtxTile, CoastType>((p, ctx) => ctx.TileInfo.HasCoast(p)));
        tileBoolSup.Register("biome", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasBiome(str)));
        tileBoolSup.Register("worldObject", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasWorldObject(str)));

        // ### Numeric suppliers for map cell context ###

        var mapNumSup = XmlDynamicValue<float, ICtxMapCell>.SupplierSpecs;

        mapNumSup.Register("fertility", ctx => ctx.Map.fertilityGrid.FertilityAt(ctx.MapCell));
        mapNumSup.Register("perlinMap", PerlinNoiseInMap);

        mapNumSup.InheritFrom(tileNumSup);

        // ### Boolean suppliers for map cell context ###

        var mapBoolSup = XmlDynamicValue<bool, ICtxMapCell>.SupplierSpecs;

        mapBoolSup.Register("terrain", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasTerrain(str)));
        mapBoolSup.Register("terrainTag", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasTerrainTag(str)));
        mapBoolSup.Register("roof", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasRoof(str)));

        mapBoolSup.InheritFrom(tileBoolSup);
    }

    private static Supplier<float, ICtxEarlyTile> PerlinNoiseInWorld(XmlNode node)
        => PerlinNoise<ICtxEarlyTile>(node, ctx => ctx.World.info.Seed, ctx => ctx.World.grid.GetTileCenter(ctx.TileId));

    private static Supplier<float, ICtxMapCell> PerlinNoiseInMap(XmlNode node)
        => PerlinNoise<ICtxMapCell>(node, ctx => Gen.HashCombineInt(ctx.World.info.Seed, ctx.TileId), ctx => ctx.MapCell.ToVec3());

    private static Supplier<float, TC> PerlinNoise<TC>(XmlNode node, Func<TC, int> seedFunc, Func<TC, Vector3> posFunc)
    {
        var frequency = node.GetNamedChild("frequency", float.Parse, 0.2f);
        var lacunarity = node.GetNamedChild("lacunarity", float.Parse, 2);
        var persistence = node.GetNamedChild("persistence", float.Parse, 0.5f);
        var seedMask = node.GetNamedChild("seedMask", GenText.StableStringHash, 0);
        var quality = node.GetNamedChild("quality", str => (QualityMode) Enum.Parse(typeof(QualityMode), str), QualityMode.Low);
        var octaves = node.GetNamedChild("octaves", int.Parse, 6);

        return ctx =>
        {
            var pos = posFunc(ctx);
            var seed = Gen.HashCombineInt(seedFunc(ctx), seedMask);
            return (float) Perlin.GetValue(pos.x, pos.y, pos.z, frequency, seed, lacunarity, persistence, octaves, quality);
        };
    }

    private static Supplier<float, ICtxEarlyTile> RandomValueInWorld(XmlNode node)
        => RandomValue<ICtxEarlyTile>(node, ctx => Gen.HashCombineInt(ctx.World.info.Seed, ctx.TileId));

    private static Supplier<float, TC> RandomValue<TC>(XmlNode node, Func<TC, int> seedFunc)
    {
        var min = node.GetNamedChild("min", float.Parse, 0f);
        var max = node.GetNamedChild("max", float.Parse, 1f);
        var seedMask = node.GetNamedChild("seedMask", GenText.StableStringHash, 0);

        return ctx =>
        {
            var seed = Gen.HashCombineInt(seedFunc(ctx), seedMask);
            return new FloatRange(min, max).RandomInRangeSeeded(seed);
        };
    }

    private static float GetCaveSystemDepthAt(ICtxEarlyTile ctx)
    {
        var landformData = ctx.World.LandformData();
        if (landformData != null) return landformData.GetCaveSystemDepthAt(ctx.TileId);

        if (Patch_RimWorld_WorldGenStep_Terrain.LastWorld == ctx.World)
        {
            var caveSystems = Patch_RimWorld_WorldGenStep_Terrain.CaveSystems;
            if (caveSystems != null && ctx.TileId >= 0 && ctx.TileId < caveSystems.Length)
            {
                return caveSystems[ctx.TileId];
            }
        }

        return 0;
    }
}

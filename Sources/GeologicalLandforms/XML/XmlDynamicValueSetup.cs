using System;
using System.Collections.Generic;
using System.Xml;
using GeologicalLandforms.Defs;
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
        // ### Numeric specs for early world tile context ###

        var earlyNumSup = XmlDynamicValue<float, ICtxEarlyTile>.SupplierSpecs;
        var earlyNumMod = XmlDynamicValue<float, ICtxEarlyTile>.ModifierSpecs;

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

        earlyNumSup.RegisterBasicNumericSuppliers();
        earlyNumMod.RegisterBasicNumericModifiers();

        // ### Boolean specs for early world tile context ###

        var earlyBoolSup = XmlDynamicValue<bool, ICtxEarlyTile>.SupplierSpecs;
        var earlyBoolMod = XmlDynamicValue<bool, ICtxEarlyTile>.ModifierSpecs;

        earlyBoolSup.Register("valueInRange", ValueInRange<float, ICtxEarlyTile>());
        earlyBoolSup.RegisterFallback(ValueInRange<float, ICtxEarlyTile>);

        earlyBoolSup.RegisterBasicBoolSuppliers();
        earlyBoolMod.RegisterBasicBoolModifiers();

        // ### Numeric specs for full world tile context ###

        var tileNumSup = XmlDynamicValue<float, ICtxTile>.SupplierSpecs;
        var tileNumMod = XmlDynamicValue<float, ICtxTile>.ModifierSpecs;

        tileNumSup.Register("borderingBiomes", ctx => ctx.TileInfo.BorderingBiomesCount());
        tileNumSup.Register("river", ctx => ctx.TileInfo.MainRiverSize());
        tileNumSup.Register("road", ctx => ctx.TileInfo.MainRoadSize());
        tileNumSup.Register("mapSize", ctx => ctx.TileInfo.ExpectedMapSize);
        tileNumSup.Register("topologyValue", ctx => ctx.TileInfo.TopologyValue);

        tileNumSup.RegisterBasicNumericSuppliers();
        tileNumMod.RegisterBasicNumericModifiers();

        tileNumSup.InheritFrom(earlyNumSup);
        tileNumMod.InheritFrom(earlyNumMod);

        // ### String specs for full world tile context ###

        var tileStringSup = XmlDynamicValue<string, ICtxTile>.SupplierSpecs;
        var tileStringMod = XmlDynamicValue<string, ICtxTile>.ModifierSpecs;

        tileStringSup.RegisterBasicStringSuppliers();
        tileStringMod.RegisterBasicStringModifiers();

        tileStringSup.RegisterFallback(Convert<string, float, ICtxTile>(v => v.ToString("0.##")));

        // ### Boolean specs for full world tile context ###

        var tileBoolSup = XmlDynamicValue<bool, ICtxTile>.SupplierSpecs;
        var tileBoolMod = XmlDynamicValue<bool, ICtxTile>.ModifierSpecs;

        tileBoolSup.Register("landform", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasLandform(str)));
        tileBoolSup.Register("topology", SupplierWithParam<bool, ICtxTile, Topology>((p, ctx) => ctx.TileInfo.IsTopologyCompatible(p)));
        tileBoolSup.Register("coast", SupplierWithParam<bool, ICtxTile, CoastType>((p, ctx) => ctx.TileInfo.HasCoast(p)));
        tileBoolSup.Register("biome", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasBiome(str)));
        tileBoolSup.Register("worldObject", SupplierWithParam<bool, ICtxTile>((str, ctx) => ctx.TileInfo.HasWorldObject(str)));

        tileBoolSup.Register("valueInRange", ValueInRange<float, ICtxTile>());
        tileBoolSup.RegisterFallback(ValueInRange<float, ICtxTile>);

        tileBoolSup.RegisterBasicBoolSuppliers();
        tileBoolMod.RegisterBasicBoolModifiers();

        tileBoolSup.InheritFrom(earlyBoolSup);
        tileBoolMod.InheritFrom(earlyBoolMod);

        // ### List specs for full world tile context ###

        XmlDynamicValue<List<DynamicBiomePlantRecord>, ICtxTile>.SupplierSpecs.RegisterBasicListSuppliers();
        XmlDynamicValue<List<DynamicBiomePlantRecord>, ICtxTile>.ModifierSpecs.RegisterBasicListModifiers();

        XmlDynamicValue<List<DynamicBiomeAnimalRecord>, ICtxTile>.SupplierSpecs.RegisterBasicListSuppliers();
        XmlDynamicValue<List<DynamicBiomeAnimalRecord>, ICtxTile>.ModifierSpecs.RegisterBasicListModifiers();

        // ### Numeric specs for map cell context ###

        var mapNumSup = XmlDynamicValue<float, ICtxMapCell>.SupplierSpecs;
        var mapNumMod = XmlDynamicValue<float, ICtxMapCell>.ModifierSpecs;

        mapNumSup.Register("fertility", ctx => ctx.Map.fertilityGrid.FertilityAt(ctx.MapCell));
        mapNumSup.Register("perlinMap", PerlinNoiseInMap);

        mapNumSup.RegisterBasicNumericSuppliers();
        mapNumMod.RegisterBasicNumericModifiers();

        mapNumSup.InheritFrom(tileNumSup);
        mapNumMod.InheritFrom(tileNumMod);

        // ### Boolean specs for map cell context ###

        var mapBoolSup = XmlDynamicValue<bool, ICtxMapCell>.SupplierSpecs;
        var mapBoolMod = XmlDynamicValue<bool, ICtxMapCell>.ModifierSpecs;

        mapBoolSup.Register("terrain", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasTerrain(str)));
        mapBoolSup.Register("terrainTag", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasTerrainTag(str)));
        mapBoolSup.Register("roof", SupplierWithParam<bool, ICtxMapCell>((str, ctx) => ctx.HasRoof(str)));

        mapBoolSup.Register("valueInRange", ValueInRange<float, ICtxMapCell>());
        mapBoolSup.RegisterFallback(ValueInRange<float, ICtxMapCell>);

        mapBoolSup.RegisterBasicBoolSuppliers();
        mapBoolMod.RegisterBasicBoolModifiers();

        mapBoolSup.InheritFrom(tileBoolSup);
        mapBoolMod.InheritFrom(tileBoolMod);
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
}

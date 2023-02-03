using LunarFramework.XML;
using static LunarFramework.XML.XmlDynamicValueSupport;
using static GeologicalLandforms.IWorldTileInfo;

namespace GeologicalLandforms;

public static class XmlDynamicValueSpecs
{
    static XmlDynamicValueSpecs()
    {
        SetupEarlyWorldTileNumberSpec(XmlDynamicValueForEarlyWorldTile<float>.Spec);
        SetupEarlyWorldTileBoolSpec(XmlDynamicValueForEarlyWorldTile<bool>.Spec);
        SetupWorldTileNumberSpec(XmlDynamicValueForWorldTile<float>.Spec);
        SetupWorldTileBoolSpec(XmlDynamicValueForWorldTile<bool>.Spec);
        SetupMapCellNumberSpec(XmlDynamicValueForMapCell<float>.Spec);
        SetupMapCellBoolSpec(XmlDynamicValueForMapCell<bool>.Spec);
    }

    private static void SetupEarlyWorldTileNumberSpec(XmlDynamicValueSpec<float, XmlContext> spec)
    {
        spec.SupplierSpecs.SpecNameAttribute = "supplier";
        spec.ModifierSpecs.SpecNameAttribute = "operation";
        
        spec.ConditionSpec = XmlDynamicValueForEarlyWorldTile<bool>.Spec;

        spec.SupplierSpecs.DefaultSpec = FixedValue<float, XmlContext>(float.Parse);
        
        spec.SupplierSpecs.Register("hilliness", ctx => (float) ctx.Tile.hilliness);
        spec.SupplierSpecs.Register("elevation", ctx => ctx.Tile.elevation);
        spec.SupplierSpecs.Register("temperature", ctx => ctx.Tile.temperature);
        spec.SupplierSpecs.Register("rainfall", ctx => ctx.Tile.rainfall);
        spec.SupplierSpecs.Register("swampiness", ctx => ctx.Tile.swampiness);
        spec.SupplierSpecs.Register("pollution", ctx => ctx.Tile.pollution);
        
        spec.ModifierSpecs.DefaultSpec = DyadicModifier<float, XmlContext>((_, v) => v);
        
        spec.ModifierSpecs.Register("replace", spec.ModifierSpecs.DefaultSpec);
        spec.ModifierSpecs.Register("add", DyadicModifier<float, XmlContext>((a, b) => a + b));
        spec.ModifierSpecs.Register("multiply", DyadicModifier<float, XmlContext>((a, b) => a * b));
        spec.ModifierSpecs.Register("subtract", DyadicModifier<float, XmlContext>((a, b) => a - b));
        spec.ModifierSpecs.Register("divide", DyadicModifier<float, XmlContext>((a, b) => b == 0 ? 0 : a / b));
        spec.ModifierSpecs.Register("curve", InterpolationCurve);
    }
    
    private static void SetupEarlyWorldTileBoolSpec(XmlDynamicValueSpec<bool, XmlContext> spec)
    {
        spec.SupplierSpecs.DefaultSpec = FixedValue<bool, XmlContext>(bool.Parse);
        
        spec.SupplierSpecs.Register("allOf", AggregateSupplierList<bool, XmlContext>((a, b) => a && b));
        spec.SupplierSpecs.Register("anyOf", AggregateSupplierList<bool, XmlContext>((a, b) => a || b));
        spec.SupplierSpecs.RegisterFallback(s => ValueInRange(s, XmlDynamicValueForEarlyWorldTile<float>.Spec));
        
        spec.ModifierSpecs.DefaultSpec = DyadicModifier((_, v) => v, spec.SupplierSpecs.GetOrThrow("allOf"));
    }
    
    private static void SetupWorldTileNumberSpec(XmlDynamicValueSpec<float, XmlContext> spec)
    {
        spec.InheritFrom(XmlDynamicValueForEarlyWorldTile<float>.Spec);
        spec.ConditionSpec = XmlDynamicValueForWorldTile<bool>.Spec;

        spec.SupplierSpecs.Register("borderingBiomes", ctx => ctx.TileInfo.BorderingBiomesCount());
        spec.SupplierSpecs.Register("river", ctx => ctx.TileInfo.MainRiverSize());
        spec.SupplierSpecs.Register("road", ctx => ctx.TileInfo.MainRoadSize());
        spec.SupplierSpecs.Register("mapSize", ctx => ctx.TileInfo.ExpectedMapSize);
    }
    
    private static void SetupWorldTileBoolSpec(XmlDynamicValueSpec<bool, XmlContext> spec)
    {
        spec.InheritFrom(XmlDynamicValueForEarlyWorldTile<bool>.Spec);
        
        spec.SupplierSpecs.Register("landform", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.TileInfo.HasLandform(str)));
        spec.SupplierSpecs.Register("topology", SupplierWithParam<bool, XmlContext, Topology>((p, ctx) => ctx.TileInfo.IsTopologyCompatible(p)));
        spec.SupplierSpecs.Register("coast", SupplierWithParam<bool, XmlContext, CoastType>((p, ctx) => ctx.TileInfo.HasCoast(p)));
        spec.SupplierSpecs.Register("biome", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.TileInfo.HasBiome(str)));
        spec.SupplierSpecs.Register("worldObject", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.TileInfo.HasWorldObject(str)));
        spec.SupplierSpecs.RegisterFallback(s => ValueInRange(s, XmlDynamicValueForWorldTile<float>.Spec));
    }
    
    private static void SetupMapCellNumberSpec(XmlDynamicValueSpec<float, XmlContext> spec)
    {
        spec.InheritFrom(XmlDynamicValueForWorldTile<float>.Spec);
        spec.ConditionSpec = XmlDynamicValueForMapCell<bool>.Spec;
        
        spec.SupplierSpecs.Register("fertility", ctx => ctx.Map.fertilityGrid.FertilityAt(ctx.MapCell));
    }
    
    private static void SetupMapCellBoolSpec(XmlDynamicValueSpec<bool, XmlContext> spec)
    {
        spec.InheritFrom(XmlDynamicValueForWorldTile<bool>.Spec);
        
        spec.SupplierSpecs.Register("terrain", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.HasTerrain(str)));
        spec.SupplierSpecs.Register("terrainTag", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.HasTerrainTag(str)));
        spec.SupplierSpecs.Register("roof", SupplierWithParam<bool, XmlContext>((str, ctx) => ctx.HasRoof(str)));
        spec.SupplierSpecs.RegisterFallback(s => ValueInRange(s, XmlDynamicValueForMapCell<float>.Spec));
    }
}

public class XmlDynamicValueForEarlyWorldTile<T> : XmlDynamicValue<T, XmlContext>
{
    public static readonly XmlDynamicValueSpec<T, XmlContext> Spec = new();
    protected override XmlDynamicValueSpec<T, XmlContext> RootSpec => Spec;
}

public class XmlDynamicValueForWorldTile<T> : XmlDynamicValue<T, XmlContext>
{
    public static readonly XmlDynamicValueSpec<T, XmlContext> Spec = new();
    protected override XmlDynamicValueSpec<T, XmlContext> RootSpec => Spec;
}

public class XmlDynamicValueForMapCell<T> : XmlDynamicValue<T, XmlContext>
{
    public static readonly XmlDynamicValueSpec<T, XmlContext> Spec = new();
    protected override XmlDynamicValueSpec<T, XmlContext> RootSpec => Spec;
}

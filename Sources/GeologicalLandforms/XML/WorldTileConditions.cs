using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class WorldTileConditions
{
    public delegate bool Condition(IWorldTileInfo tile);
    
    private List<Condition> _conditions = new();

    public bool Evaluate(IWorldTileInfo tile)
    {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var condition in _conditions) if (!condition(tile)) return false;
        return true;
    }
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        _conditions = LoadConditionsFromXml(xmlRoot);
    }

    private static List<Condition> LoadConditionsFromXml(XmlNode xmlRoot)
    {
        return (from XmlNode childNode in xmlRoot.ChildNodes select LoadConditionFromXml(childNode)).ToList();
    }

    private static Condition LoadConditionFromXml(XmlNode childNode)
    {
        return childNode.Name switch
        {
            "anyOf" => AnyOf(LoadConditionsFromXml(childNode)),
            "allOf" => AllOf(LoadConditionsFromXml(childNode)),
            "landform" => LandformsAnyOf(childNode.AsStringList().ToList()),
            "biome" => BiomeAnyOf(childNode.AsStringList().ToList()),
            "topology" => TopologyCompatible(Enum.TryParse(childNode.InnerText, out Topology t) ? t : Topology.Any),
            "coast" => Coast(Enum.TryParse(childNode.InnerText, out IWorldTileInfo.CoastType t) ? t : IWorldTileInfo.CoastType.Ocean),
            "hilliness" => Hilliness(FloatRange.FromString(childNode.InnerText)),
            "elevation" => Elevation(FloatRange.FromString(childNode.InnerText)),
            "temperature" => Temperature(FloatRange.FromString(childNode.InnerText)),
            "rainfall" => Rainfall(FloatRange.FromString(childNode.InnerText)),
            "swampiness" => Swampiness(FloatRange.FromString(childNode.InnerText)),
            "borderingBiomes" => BorderingBiomes(FloatRange.FromString(childNode.InnerText)),
            "river" => River(FloatRange.FromString(childNode.InnerText)),
            "road" => Road(FloatRange.FromString(childNode.InnerText)),
            "settlement" => Settlement(bool.Parse(childNode.InnerText)),
            "questSite" => QuestSite(bool.Parse(childNode.InnerText)),
            "expectedMapSize" => ExpectedMapSize(FloatRange.FromString(childNode.InnerText)),
            _ => throw new Exception("unknown requirement type: " + childNode.Name)
        };
    }

    public static Condition AnyOf(List<Condition> conditions) =>
        info => conditions.Any(condition => condition(info));
    
    public static Condition AllOf(List<Condition> conditions) =>
        info => conditions.All(condition => condition(info));
    
    public static Condition LandformsAnyOf(List<string> landforms) =>
        info => info.Landforms != null && info.Landforms.Any(l => landforms.Contains(l.Id));

    public static Condition BiomeAnyOf(List<string> biomes) =>
        info => biomes.Contains(info.Biome.defName);

    public static Condition TopologyCompatible(Topology topology) =>
        info => topology.IsCompatible(info.Topology, false);
    
    public static Condition Coast(IWorldTileInfo.CoastType coast) =>
        info => info.Coast.Any(c => c == coast);
    
    public static Condition Hilliness(FloatRange range) =>
        info => range.Includes((float) info.Hilliness);
    
    public static Condition Elevation(FloatRange range) =>
        info => range.Includes(info.Elevation);
    
    public static Condition Temperature(FloatRange range) =>
        info => range.Includes(info.Temperature);
    
    public static Condition Rainfall(FloatRange range) =>
        info => range.Includes(info.Rainfall);
    
    public static Condition Swampiness(FloatRange range) =>
        info => range.Includes(info.Swampiness);
    
    public static Condition BorderingBiomes(FloatRange range) =>
        info => range.Includes(info.BorderingBiomes?.Count ?? 0);
    
    public static Condition River(FloatRange range) =>
        info => range.Includes(info.MainRiver?.widthOnWorld ?? 0f);
    
    public static Condition Road(FloatRange range) =>
        info => range.Includes(1f - (info.MainRoad?.movementCostMultiplier ?? 1f));
    
    public static Condition Settlement(bool expected) =>
        info => info.WorldObject is Settlement { Faction.IsPlayer: false } == expected;
    
    public static Condition QuestSite(bool expected) =>
        info => info.WorldObject is Site { Faction.IsPlayer: false } == expected;

    public static Condition ExpectedMapSize(FloatRange range) 
        => info => range.Includes((info.WorldObject is Site site ? site.PreferredMapSize : Find.World.info.initialMapSize).MinXZ());
}
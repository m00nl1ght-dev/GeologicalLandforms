using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace GeologicalLandforms;

public class MapGridConditions
{
    public delegate bool Condition(IntVec3 pos);
    public delegate Condition ConditionBuilder(Map map);
    
    private List<ConditionBuilder> _conditions = new();
    public bool HasConditions => _conditions.Count > 0;

    public void Evaluate(Map map, Action<IntVec3> action)
    {
        var list = _conditions.Select(c => c(map)).ToList();
        foreach (var cell in map.AllCells)
        {
            if (list.All(c => c(cell))) action(cell);
        }
    }
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        _conditions = LoadConditionsFromXml(xmlRoot);
    }

    private static List<ConditionBuilder> LoadConditionsFromXml(XmlNode xmlRoot)
    {
        return (from XmlNode childNode in xmlRoot.ChildNodes select LoadConditionFromXml(childNode)).ToList();
    }

    private static ConditionBuilder LoadConditionFromXml(XmlNode childNode)
    {
        return childNode.Name switch
        {
            "anyOf" => AnyOf(LoadConditionsFromXml(childNode)),
            "allOf" => AllOf(LoadConditionsFromXml(childNode)),
            "terrain" => TerrainAnyOf(childNode.AsStringList().ToList()),
            "terrainTag" => TerrainHasTag(childNode.InnerText),
            "roof" => RoofAnyOf(childNode.AsStringList().ToList()),
            _ => throw new Exception("unknown requirement type: " + childNode.Name)
        };
    }

    public static ConditionBuilder AnyOf(List<ConditionBuilder> conditions) => map =>
    {
        var list = conditions.Select(c => c(map)).ToList();
        return pos => list.Any(condition => condition(pos));
    };
    
    public static ConditionBuilder AllOf(List<ConditionBuilder> conditions) => map =>
    {
        var list = conditions.Select(c => c(map)).ToList();
        return pos => list.All(condition => condition(pos));
    };

    public static ConditionBuilder TerrainAnyOf(List<string> terrainDefs) =>
        map => pos => terrainDefs.Contains(map.terrainGrid.TerrainAt(pos)?.defName);

    public static ConditionBuilder TerrainHasTag(string tag) =>
        map => pos => map.terrainGrid.TerrainAt(pos)?.tags?.Contains(tag) ?? false;
    
    public static ConditionBuilder RoofAnyOf(List<string> roofDefs) =>
        map => pos => roofDefs.Contains(map.roofGrid.RoofAt(pos)?.defName ?? "Unroofed");
}
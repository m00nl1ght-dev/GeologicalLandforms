using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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
    
    private static BiomeProperties[] _biomeProperties;
    
    public static BiomeProperties Properties(this BiomeDef biomeDef)
    {
        try
        {
            return GeologicalLandformsAPI.ApplyBiomePropertiesHook(biomeDef, _biomeProperties[biomeDef.index]);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return new BiomeProperties();
        }
    }

    public static void Init()
    {
        _biomeProperties = BiomeProperties.GetAll();
    }

    public static void ClearCaches()
    {
        _landformDataCache = null;
        _biomeGridCache = null;
    }
    
    public static T Get<T>(this T[,] grid, IntVec3 c) => grid[c.x, c.z];

    public static void Set<T>(this T[,] grid, IntVec3 c, T value) => grid[c.x, c.z] = value;

    public static IEnumerable<string> AsStringList(this XmlNode node) =>
        node.HasChildNodes ? node.ChildNodes.Cast<XmlNode>().Select(n => n.InnerText) : new []{ node.InnerText };
    
    public static IEnumerable<T> AsObjectList<T>(this XmlNode node, Func<string, T> parser) =>
        AsStringList(node).Select(parser).Where(n => n != null);
}
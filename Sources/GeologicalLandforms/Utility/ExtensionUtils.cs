using System;
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
}
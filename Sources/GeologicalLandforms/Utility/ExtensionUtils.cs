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
}
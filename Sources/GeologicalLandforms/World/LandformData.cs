using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class LandformData : WorldComponent
{
    private Dictionary<int, TileData> _tileData = new();
    private bool[] _biomeTransitions = Array.Empty<bool>();
    private byte[] _caveSystems = Array.Empty<byte>();

    public LandformData(World world) : base(world) { }

    public bool TryGet(int tileId, out TileData data)
    {
        var hasData = _tileData.TryGetValue(tileId, out data);
        if (data != null) data = new TileData(data);
        return hasData;
    }

    public bool HasData(int tileId)
    {
        return _tileData.ContainsKey(tileId);
    }

    public void Commit(int tileId, TileData data)
    {
        _tileData[tileId] = new TileData(data);
        MapPreviewAPI.NotifyWorldChanged();
    }

    public void Reset(int tileId)
    {
        _tileData.Remove(tileId);
        MapPreviewAPI.NotifyWorldChanged();
    }

    public void ResetAll()
    {
        _tileData.Clear();
        MapPreviewAPI.NotifyWorldChanged();
    }

    public bool HasBiomeTransitions()
    {
        return _biomeTransitions.Length > 0;
    }

    public bool GetBiomeTransition(int tileId, int nbId)
    {
        if (tileId < 0 || tileId >= world.grid.TilesCount || nbId is < 0 or > 5) return false;
        return _biomeTransitions[tileId * 6 + nbId];
    }

    public void SetBiomeTransitions(bool[] transitions)
    {
        _biomeTransitions = transitions ?? Array.Empty<bool>();
    }

    public bool HasCaveSystems()
    {
        return _caveSystems.Length > 0;
    }

    public byte GetCaveSystemDepthAt(int tileId)
    {
        if (tileId < 0 || tileId >= _caveSystems.Length) return 0;
        return _caveSystems[tileId];
    }

    public void SetCaveSystems(byte[] caveSystems)
    {
        _caveSystems = caveSystems ?? Array.Empty<byte>();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _tileData, "tileData", LookMode.Value, LookMode.Deep);

        var hasBiomeTransitions = HasBiomeTransitions();

        Scribe_Values.Look(ref hasBiomeTransitions, "hasBiomeTransitions");

        if (hasBiomeTransitions)
        {
            var elements = world.grid.TilesCount * 6;
            if (_biomeTransitions.Length != elements) _biomeTransitions = new bool[elements];
            DataExposeUtility.BoolArray(ref _biomeTransitions, elements, "biomeTransitions");
        }

        var hasCaveSystems = HasCaveSystems();

        Scribe_Values.Look(ref hasCaveSystems, "hasCaveSystems");

        if (hasCaveSystems)
        {
            var elements = world.grid.TilesCount;
            if (_caveSystems.Length != elements) _caveSystems = new byte[elements];
            DataExposeUtility.ByteArray(ref _caveSystems, "caveSystems");
        }

        _tileData ??= new();
        _biomeTransitions ??= Array.Empty<bool>();
        _caveSystems ??= Array.Empty<byte>();

        ExtensionUtils.ClearCaches();
    }

    public class TileData : IExposable
    {
        public Topology Topology;
        public float TopologyValue;
        public Rot4 TopologyDirection;

        public List<string> Landforms;
        public List<string> BiomeVariants;

        public TileData() { }

        public TileData(IWorldTileInfo tileInfo)
        {
            Topology = tileInfo.Topology;
            TopologyValue = tileInfo.TopologyValue;
            TopologyDirection = tileInfo.TopologyDirection;
            Landforms = tileInfo.Landforms?.Select(lf => lf.Id).ToList();
            BiomeVariants = tileInfo.BiomeVariants?.Select(bv => bv.defName).ToList();
        }

        public TileData(TileData other)
        {
            Topology = other.Topology;
            TopologyValue = other.TopologyValue;
            TopologyDirection = other.TopologyDirection;
            Landforms = other.Landforms?.ToList();
            BiomeVariants = other.BiomeVariants?.ToList();
        }

        public void Apply(WorldTileInfoPrimer primer)
        {
            primer.Topology = Topology;
            primer.TopologyValue = TopologyValue;
            primer.TopologyDirection = TopologyDirection;
            primer.Landforms = Landforms?.Select(ResolveLandform).Where(lf => lf != null).OrderBy(e => e.Priority).ToList();
            primer.BiomeVariants = BiomeVariants?.Select(ResolveBiomeVariant).Where(bv => bv != null).ToList();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Topology, "topology");
            Scribe_Values.Look(ref TopologyValue, "topologyValue");
            Scribe_Values.Look(ref TopologyDirection, "topologyDirection", Rot4.North);
            Scribe_Collections.Look(ref Landforms, "landforms", LookMode.Value);
            Scribe_Collections.Look(ref BiomeVariants, "biomeVariants", LookMode.Value);
        }
    }

    internal static Landform ResolveLandform(string id)
    {
        var landform = LandformManager.FindById(id);
        if (landform != null) return landform;

        GeologicalLandformsAPI.Logger.Warn("Could not resolve landform: " + id);
        return null;
    }

    internal static BiomeVariantDef ResolveBiomeVariant(string defName)
    {
        var def = DefDatabase<BiomeVariantDef>.GetNamed(defName);
        if (def != null) return def;

        GeologicalLandformsAPI.Logger.Warn("Could not resolve biome variant: " + defName);
        return null;
    }
}

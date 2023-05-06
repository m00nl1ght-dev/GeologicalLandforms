using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using MapPreview;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class LandformData : WorldComponent
{
    private Dictionary<int, Entry> _entries = new();
    private bool[] _biomeTransitions = Array.Empty<bool>();
    private byte[] _caveSystems = Array.Empty<byte>();

    public LandformData(World world) : base(world) { }

    public bool TryGet(int tileId, out Entry entry)
    {
        return _entries.TryGetValue(tileId, out entry);
    }

    public bool IsLocked(int tileId)
    {
        if (!_entries.TryGetValue(tileId, out var entry)) return false;
        return entry.Locked;
    }

    public void Commit(int tileId, IWorldTileInfo worldTileInfo, bool locked = false)
    {
        Commit(tileId, worldTileInfo.Landforms?.LastOrDefault(v => !v.IsLayer), worldTileInfo.TopologyDirection, locked);
    }

    public void Commit(int tileId, Landform landform, Rot4 landformDirection, bool locked = false)
    {
        _entries[tileId] = new Entry { LandformId = landform?.Id, LandformDirectionInt = landformDirection.AsInt, Locked = locked };
        MapPreviewAPI.NotifyWorldChanged();
    }

    public void Reset(int tileId)
    {
        _entries.Remove(tileId);
        MapPreviewAPI.NotifyWorldChanged();
    }

    public void ResetAll()
    {
        _entries.Clear();
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

    static LandformData() => ParseHelper.Parsers<Entry>.Register(Entry.FromString);

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _entries, "entries", LookMode.Value, LookMode.Value);

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

        _entries ??= new();
        _biomeTransitions ??= Array.Empty<bool>();
        _caveSystems ??= Array.Empty<byte>();

        ExtensionUtils.ClearCaches();
    }

    public struct Entry
    {
        public string LandformId;
        public int LandformDirectionInt;
        public bool Locked;

        public Landform Landform => LandformId == null ? null : LandformManager.FindById(LandformId);
        public Rot4 LandformDirection => new(LandformDirectionInt);

        public static Entry FromString(string s)
        {
            return new Entry
            {
                LandformId = s.Length > 2 ? s.Substring(2) : null,
                LandformDirectionInt = Convert.ToInt32(s[1]),
                Locked = s[0] == 'L'
            };
        }

        public override string ToString() => (Locked ? 'L' : 'U').ToString() + LandformDirectionInt + (LandformId ?? "");
    }
}

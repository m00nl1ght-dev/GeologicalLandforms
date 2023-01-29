using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Utility;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class BiomeGrid : MapComponent
{
    private readonly Entry[] _grid;

    private List<Entry> _entries = new();
    public IReadOnlyList<Entry> Entries => _entries;
    public Entry Primary => _entries[0];

    private bool _enabled;
    public bool Enabled => _enabled;
    public void Enable() => _enabled = true;
    
    public float OpenGroundFraction { get; private set; } = 1f;

    private readonly IntVec3 _mapSize;

    internal object LoadId = new();

    public BiomeGrid(Map map) : this(map, map.Size, map.Parent == null ? BiomeDefOf.TemperateForest : map.Biome) {}
    
    public BiomeGrid(Map map, IntVec3 mapSize, BiomeDef worldBiome) : base(map)
    {
        _mapSize = mapSize;
        _grid = new Entry[_mapSize.x * _mapSize.z];
        var primary = MakeEntry(worldBiome);
        primary.CellCount = _mapSize.x * _mapSize.z;
    }

    public Entry EntryAt(int cell)
    {
        if (cell < 0 || cell >= _grid.Length) return Primary;
        return _grid[cell] ?? Primary;
    }
    
    public Entry EntryAt(IntVec3 c)
    {
        return EntryAt(CellIndicesUtility.CellToIndex(c, _mapSize.x));
    }

    public BiomeDef BiomeAt(int cell) => EntryAt(cell).Biome;
    public BiomeDef BiomeAt(IntVec3 c) => EntryAt(c).Biome;

    public void SetEntry(IntVec3 c, Entry entry)
    {
        if (entry.LoadId != LoadId) throw new Exception("LoadId mismatch");
        int i = CellIndicesUtility.CellToIndex(c, _mapSize.x);
        var old = EntryAt(i);
        _grid[i] = entry;
        old.CellCount--;
        entry.CellCount++;
    }

    public void SetBiome(IntVec3 c, BiomeDef biome) => SetEntry(c, MakeEntry(biome));

    public void SetBiomes(IGridFunction<BiomeData> biomeFunction)
    {
        var primary = Primary;
        var last = primary;
        
        foreach (var entry in _entries) entry.CellCount = 0;

        for (int x = 0; x < _mapSize.x; x++) for (int z = 0; z < _mapSize.z; z++)
        {
            var c = new IntVec3(x, 0, z);
            var biomeDef = biomeFunction.ValueAt(c.x, c.z).Biome;
            int i = CellIndicesUtility.CellToIndex(c, _mapSize.x);
            _grid[i] = last = last.Biome == biomeDef ? last : MakeEntry(biomeDef);
            last.CellCount++;
        }
    }

    public void ApplyVariantLayers(IEnumerable<BiomeVariantLayer> layers)
    {
        foreach (var layer in layers)
        {
            if (layer.mapGridConditions.HasConditions)
            {
                var entryCache = new Entry[_entries.Count];
                layer.mapGridConditions.Evaluate(map, pos =>
                {
                    var oldEntry = EntryAt(pos);
                    var newEntry = entryCache[oldEntry.Index];
                    int cellIdx = CellIndicesUtility.CellToIndex(pos, _mapSize.x);
                    newEntry ??= entryCache[oldEntry.Index] = MakeEntry(oldEntry.BiomeBase, oldEntry.VariantLayers.Append(layer).ToList());
                    _grid[cellIdx] = newEntry;
                    oldEntry.CellCount--;
                    newEntry.CellCount++;
                });
            }
            else
            {
                foreach (var entry in _entries)
                {
                    entry.Add(layer);
                }
            }
        }

        foreach (var entry in _entries)
        {
            entry.Refresh();
        }
    }

    public void UpdateOpenGroundFraction()
    {
        if (map == null) return;
        float total = map.cellIndices.NumGridCells;
        float walkable = map.AllCells.Sum(GetOpenGroundFractionFor);
        OpenGroundFraction = Mathf.Clamp01(walkable / total);
    }

    private float GetOpenGroundFractionFor(IntVec3 cell)
    {
        if (map.terrainGrid.TerrainAt(cell).IsNormalWater()) return 0.1f;
        if (cell.Walkable(map)) return 1f;
        return 0.35f;
    }

    public Entry MakeEntry(BiomeDef biomeBase, List<BiomeVariantLayer> variantLayers = null)
    {
        if (biomeBase == null) return Primary;
        
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var entry in _entries) if (entry.IsEquivalent(biomeBase, variantLayers)) return entry;

        var newEntry = new Entry { Index = (ushort) _entries.Count, LoadId = LoadId };
        _entries.Add(newEntry);
        
        newEntry.Set(biomeBase, variantLayers);
        newEntry.Refresh();
        return newEntry;
    }

    public override void ExposeData()
    {
        if (map == null) return;

        if (Scribe.mode == LoadSaveMode.LoadingVars) LoadId = new();

        Scribe_Values.Look(ref _enabled, "enabled");
        Scribe_Collections.Look(ref _entries, "entries", LookMode.Deep);

        var isLegacy = _entries == null || _entries.Count == 0;
        Dictionary<ushort, BiomeDef> biomeDefsByShortHash = null;
        
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (isLegacy)
            {
                GeologicalLandformsAPI.Logger.Log("Upgrading BiomeGrid data from old version.");
                biomeDefsByShortHash = DefDatabase<BiomeDef>.AllDefs.ToDictionary(allDef => allDef.shortHash);
                _entries = new List<Entry>();
                BiomeDef primaryBiome = null;
                Scribe_Defs.Look(ref primaryBiome, "primary");
                MakeEntry(primaryBiome ?? BiomeDefOf.TemperateForest);
            }
            else
            {
                foreach (var entry in _entries)
                {
                    entry.LoadId = LoadId;
                    entry.Refresh();
                }
            }
        }

        var primary = Primary;

        ushort ShortReader(IntVec3 c)
        {
            var entry = _grid[map.cellIndices.CellToIndex(c)];
            return entry?.Index ?? 0;
        }

        void ShortWriter(IntVec3 c, ushort val)
        {
            var entry = val >= _entries.Count ? primary : _entries[val];
            _grid[map.cellIndices.CellToIndex(c)] = entry;
            entry.CellCount++;
        }
        
        void LegacyShortWriter(IntVec3 c, ushort val)
        {
            var biome = biomeDefsByShortHash.TryGetValue(val);
            var entry = biome == null ? Primary : MakeEntry(biome);
            _grid[map.cellIndices.CellToIndex(c)] = entry;
            if (entry != Primary) _enabled = true;
            entry.CellCount++;
        }

        MapExposeUtility.ExposeUshort(map, ShortReader, isLegacy ? LegacyShortWriter : ShortWriter, "biomeGrid");
        
        ExtensionUtils.ClearCaches();
    }

    public override void FinalizeInit()
    {
        UpdateOpenGroundFraction();
    }

    public override string ToString()
    {
        return "BiomeGrid\n" + _entries.Join(e => e.ToString(), "\n");
    }

    public class Entry : IExposable
    {
        public ushort Index { get; internal set; }
        public int CellCount { get; internal set; }

        public BiomeDef BiomeBase => _biomeBase;
        private BiomeDef _biomeBase;
        
        public IReadOnlyList<BiomeVariantLayer> VariantLayers => _variantLayers;
        public bool HasVariants => VariantLayers.Count > 0;
        private List<BiomeVariantLayer> _variantLayers = new();
        
        public BiomeDef Biome { get; private set; }
        public bool ApplyToCaveSpawns { get; private set; }

        internal object LoadId;

        public void Set(BiomeDef biomeBase, IEnumerable<BiomeVariantLayer> variantLayers = null)
        {
            _biomeBase = biomeBase;
            _variantLayers = variantLayers?.ToList() ?? new();
        }
        
        public void Add(BiomeVariantLayer layer)
        {
            _variantLayers.AddDistinct(layer);
        }

        public void Refresh()
        {
            _biomeBase ??= BiomeDefOf.TemperateForest;
            Biome = HasVariants ? BiomeVariantLayer.Apply(_biomeBase, _variantLayers) : BiomeBase;
            ApplyToCaveSpawns = _variantLayers.Any(l => l.applyToCaveSpawns);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref _biomeBase, "biomeBase");

            List<string> list = null;
            if (Scribe.mode == LoadSaveMode.Saving) 
                list = _variantLayers.Select(l => l.ToString()).ToList();
            
            Scribe_Collections.Look(ref list, "biomeLayers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars) 
                _variantLayers = list.Select(BiomeVariantLayer.FindFromString).Where(v => v != null).ToList();
        }

        public bool IsEquivalent(BiomeDef biomeBase, List<BiomeVariantLayer> variantLayers = null)
        {
            if (BiomeBase != biomeBase) return false;
            
            var cntEntry = VariantLayers?.Count ?? 0;
            var cntGiven = variantLayers?.Count ?? 0;
            
            if (cntEntry != cntGiven) return false;
            
            for (int i = 0; i < cntEntry; i++)
            {
                if (VariantLayers![i] != variantLayers![i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return $"{CellCount} x {BiomeBase.defName} [{VariantLayers.Join(l => l.ToString())}]";
        }
    }
}
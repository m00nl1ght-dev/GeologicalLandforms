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
    public IReadOnlyList<Entry> Entries => _entries;
    public Entry Primary => _entries[0];

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    /// Represents how open (solid ground) the map is.
    /// Water yields 0.1 and rock wall yields 0.35 towards this factor.
    /// </summary>
    public float OpenGroundFraction { get; private set; } = 1f;

    internal ushort[] WalkableEdgeCellsCache;
    internal IntVec3[] UnroofedCellsCache;

    internal object LoadId = new();

    private bool _enabled;
    private List<Entry> _entries = new();

    private readonly Entry[] _grid;
    private readonly IntVec3 _mapSize;

    /// <summary>
    /// Create a new biome grid for a normal full map.
    /// </summary>
    public BiomeGrid(Map map) : this(map, map.Size, map.Parent == null ? BiomeDefOf.TemperateForest : map.Biome) { }

    /// <summary>
    /// Create a new biome grid for a map preview.
    /// </summary>
    /// <param name="map">can be null for previews</param>
    /// <param name="mapSize">the size of the preview</param>
    /// <param name="worldBiome">the biome of the world tile to be previewed</param>
    public BiomeGrid(Map map, IntVec3 mapSize, BiomeDef worldBiome) : base(map)
    {
        _mapSize = mapSize;
        _grid = new Entry[_mapSize.x * _mapSize.z];
        var primary = MakeEntry(worldBiome);
        primary.CellCount = _mapSize.x * _mapSize.z;
    }

    /// <summary>
    /// The world tile info if available, otherwise null.
    /// It may not be available for map previews and during savegame loading.
    /// </summary>
    public WorldTileInfo TileInfo => map.Tile < 0 ? null : WorldTileInfo.Get(map.Tile);

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

        for (int x = 0; x < _mapSize.x; x++)
        for (int z = 0; z < _mapSize.z; z++)
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
        if (map == null) throw new Exception("Preview maps can not have biome variants");

        var tile = TileInfo;

        foreach (var layer in layers)
        {
            var conditions = layer.mapGridConditions;

            if (conditions != null)
            {
                var entryCache = new Entry[_entries.Count];

                foreach (var pos in map.AllCells)
                {
                    if (conditions.Get(new CtxMapCell(tile, map, pos)))
                    {
                        var oldEntry = EntryAt(pos);
                        var newEntry = entryCache[oldEntry.Index];
                        int cellIdx = CellIndicesUtility.CellToIndex(pos, _mapSize.x);
                        newEntry ??= entryCache[oldEntry.Index] = MakeEntry(oldEntry.BiomeBase, oldEntry.VariantLayers.Append(layer).ToList());
                        _grid[cellIdx] = newEntry;
                        oldEntry.CellCount--;
                        newEntry.CellCount++;
                    }
                }
            }
            else
            {
                foreach (var entry in _entries)
                {
                    entry.Add(layer);
                }
            }
        }

        RefreshAllEntries();
    }

    public void UpdateOpenGroundFraction()
    {
        if (map == null) return;
        float total = map.cellIndices.NumGridCells;
        bool caveBiome = Primary.BiomeBase.Properties().applyToCaves;
        bool waterPassable = TerrainDefOf.WaterDeep.passability != Traversability.Impassable;
        float walkable = map.AllCells.Sum(c => GetOpenGroundFractionFor(c, caveBiome, waterPassable));
        OpenGroundFraction = Mathf.Clamp01(walkable / total);
    }

    private float GetOpenGroundFractionFor(IntVec3 cell, bool caveBiome, bool waterPassable)
    {
        var terrain = map.terrainGrid.TerrainAt(cell);
        if (terrain.IsNormalWater()) return waterPassable ? 0.5f : 0.1f;
        if (!cell.Walkable(map)) return caveBiome ? 0.75f : 0.35f;
        if (!terrain.natural) return 0.4f;
        return 1f;
    }

    public Entry MakeEntry(BiomeDef biomeBase, List<BiomeVariantLayer> variantLayers = null, bool forceNew = false)
    {
        if (biomeBase == null) return Primary;

        if (!forceNew)
            foreach (var entry in _entries)
                if (entry.IsEquivalent(biomeBase, variantLayers))
                    return entry;

        var newEntry = new Entry { Index = (ushort) _entries.Count, LoadId = LoadId };
        _entries.Add(newEntry);

        newEntry.Set(biomeBase, variantLayers);
        newEntry.Refresh(TileInfo);
        return newEntry;
    }

    public override void ExposeData()
    {
        if (map == null) throw new Exception("Preview maps can not be saved");

        if (Scribe.mode == LoadSaveMode.LoadingVars) LoadId = new();

        if (_entries is { Count: > 0 })
        {
            // for legacy version reverse compat
            var legacyPrimary = _entries[0].BiomeBase;
            Scribe_Defs.Look(ref legacyPrimary, "primary");
        }

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
                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];
                    entry.Index = (ushort) i;
                    entry.LoadId = LoadId;
                    entry.Refresh(null);
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
        RefreshAllEntries();
    }

    public void RefreshAllEntries()
    {
        var tileInfo = TileInfo;
        foreach (var entry in Entries) entry.Refresh(tileInfo);
    }

    public List<ThingDef> AllPotentialPlants => Entries.SelectMany(e => e.Biome.AllWildPlants).Distinct().ToList();
    public List<PawnKindDef> AllPotentialAnimals => Entries.SelectMany(e => e.Biome.AllWildAnimals).Distinct().ToList();

    public override string ToString()
    {
        return "BiomeGrid\n" + _entries.Join(e => e.ToString(), "\n");
    }

    public class Entry : IExposable
    {
        public ushort Index { get; internal set; }
        public int CellCount { get; internal set; }

        public BiomeDef BiomeBase => _biomeBase;
        private BiomeDef _biomeBase = BiomeDefOf.TemperateForest;

        public IReadOnlyList<BiomeVariantLayer> VariantLayers => _variantLayers;
        public bool HasVariants => VariantLayers.Count > 0;
        private List<BiomeVariantLayer> _variantLayers = new();

        public BiomeDef Biome { get; private set; }
        public bool ApplyToCaves { get; private set; }

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

        public void Refresh(WorldTileInfo tile)
        {
            // We can't apply the variants when called from ExposeData() because the map is not fully loaded yet (and we need the tileId)
            // So in that case tile is null, and we set the plain base biome for now, later Refresh() is called again in FinalizeInit() with the tile
            Biome = HasVariants && tile != null ? BiomeVariantLayer.Apply(tile, _biomeBase, _variantLayers) : BiomeBase;
            ApplyToCaves = BiomeBase.Properties().applyToCaves || _variantLayers.Any(l => l.applyToCaves);
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
            return $"{BiomeBase.defName}" + (VariantLayers.Count > 0 ? $" [{VariantLayers.Join(l => l.ToString())}]" : "");
        }
    }
}

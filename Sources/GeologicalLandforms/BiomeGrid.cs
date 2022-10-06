using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class BiomeGrid : MapComponent
{
    private readonly BiomeDef[] _grid;

    private Dictionary<BiomeDef, int> _cellCounts = new();
    public IReadOnlyDictionary<BiomeDef, int> CellCounts => PrimaryBiome == null ? new() : _cellCounts;
    
    public bool HasMultipleBiomes => CellCounts.Count > 1;
    public bool ShouldApply => HasMultipleBiomes && PrimaryBiome == map.Biome;
    public bool ShouldApplyForPlantSpawning => ShouldApply || _applyReplacementsForPlantSpawning;
    public bool ShouldApplyForAnimalSpawning => ShouldApply || _applyReplacementsForAnimalSpawning;
    
    public float OpenGroundFraction { get; private set; } = 1f;

    private BiomeDef[] _replacementKeys = Array.Empty<BiomeDef>();
    private BiomeDef[] _replacementValues = Array.Empty<BiomeDef>();
    private bool _applyReplacementsForPlantSpawning;
    private bool _applyReplacementsForAnimalSpawning;

    private readonly IntVec3 _mapSize;

    private BiomeDef _primary;
    public BiomeDef PrimaryBiome
    {
        get
        {
            if (_primary != null) return _primary;
            var parent = map?.info?.parent;
            var worldGrid = Find.WorldGrid;
            if (parent == null || worldGrid == null) return BiomeDefOf.TemperateForest;
            var tile = worldGrid[parent.Tile];
            if (tile == null) return BiomeDefOf.TemperateForest;
            _primary = tile.biome;
            _cellCounts[_primary] = _mapSize.x * _mapSize.z;
            return _primary;
        }
    }

    public BiomeGrid(Map map) : base(map)
    {
        _mapSize = map.Size;
        _grid = new BiomeDef[_mapSize.x * _mapSize.z];
    }
    
    public BiomeGrid(IntVec3 mapSize, BiomeDef primary) : base(null)
    {
        _mapSize = mapSize;
        _primary = primary;
        _grid = new BiomeDef[_mapSize.x * _mapSize.z];
        _cellCounts[_primary] = _mapSize.x * _mapSize.z;
    }

    public BiomeDef BiomeAt(int cell, BiomeQuery query = BiomeQuery.Generic)
    {
        if (cell < 0 || cell >= _grid.Length) return PrimaryBiome;
        var biome = _grid[cell] ?? PrimaryBiome;

        if ((query == BiomeQuery.PlantSpawning && _applyReplacementsForPlantSpawning) || 
            (query == BiomeQuery.AnimalSpawning && _applyReplacementsForAnimalSpawning))
        {
            for (var i = 0; i < _replacementKeys.Length; i++)
            {
                if (_replacementKeys[i] == biome) return _replacementValues[i];
            }
        }

        return biome;
    }
    
    public BiomeDef BiomeAt(IntVec3 c, BiomeQuery query = BiomeQuery.Generic)
    {
        return BiomeAt(CellIndicesUtility.CellToIndex(c, _mapSize.x), query);
    }

    public void SetBiome(IntVec3 c, BiomeDef biomeDef)
    {
        int i = CellIndicesUtility.CellToIndex(c, _mapSize.x);
        var old = BiomeAt(i);
        _cellCounts.TryGetValue(old, out var oldCount); 
        _cellCounts[old] = Math.Max(0, oldCount - 1);
        _grid[i] = biomeDef;
        _cellCounts.TryGetValue(biomeDef, out var newCount); 
        _cellCounts[biomeDef] = newCount + 1;
    }

    public void SetBiomes(IGridFunction<BiomeData> biomeFunction)
    {
        var primaryBiome = PrimaryBiome;
        _cellCounts.Clear();
        for (int x = 0; x < _mapSize.x; x++) for (int z = 0; z < _mapSize.z; z++)
        {
            var c = new IntVec3(x, 0, z);
            var biomeDef = biomeFunction.ValueAt(c.x, c.z).Biome ?? primaryBiome;
            int i = CellIndicesUtility.CellToIndex(c, _mapSize.x);
            _grid[i] = biomeDef;
            _cellCounts.TryGetValue(biomeDef, out var newCount); 
            _cellCounts[biomeDef] = newCount + 1;
        }
    }

    public void SetReplacements(Dictionary<BiomeDef, BiomeDef> replacements, ICollection<BiomeQuery> applyTo)
    {
        _replacementKeys = new BiomeDef[replacements.Count];
        _replacementValues = new BiomeDef[replacements.Count];

        int i = 0;
        foreach (var pair in replacements)
        {
            _replacementKeys[i] = pair.Key;
            _replacementValues[i] = pair.Value;
            i++;
        }

        _applyReplacementsForPlantSpawning = applyTo.Contains(BiomeQuery.PlantSpawning);
        _applyReplacementsForAnimalSpawning = applyTo.Contains(BiomeQuery.AnimalSpawning);
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

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref _primary, "primary");
        var biomeDefsByShortHash = DefDatabase<BiomeDef>.AllDefs.ToDictionary(allDef => allDef.shortHash);
        ExposeBiomeArray(biomeDefsByShortHash, _grid, "biomeGrid");
        Scribe_Collections.Look(ref _cellCounts, "cellCounts", LookMode.Def, LookMode.Value);
        
        Scribe_Values.Look(ref _applyReplacementsForPlantSpawning, "applyReplacementsForPlantSpawning");
        Scribe_Values.Look(ref _applyReplacementsForAnimalSpawning, "applyReplacementsForAnimalSpawning");

        if (_applyReplacementsForAnimalSpawning || _applyReplacementsForPlantSpawning)
        {
            var keys = _replacementKeys.ToList();
            var values = _replacementValues.ToList();
            Scribe_Collections.Look(ref keys, "replacementKeys", LookMode.Def);
            Scribe_Collections.Look(ref values, "replacementValues", LookMode.Def);
            _replacementKeys = keys.ToArray();
            _replacementValues = values.ToArray();
        }
    }

    private void ExposeBiomeArray(Dictionary<ushort, BiomeDef> biomeDefsByShortHash, BiomeDef[] array, string name)
    {
        if (map == null) return;
        MapExposeUtility.ExposeUshort(map, c => array[map.cellIndices.CellToIndex(c)]?.shortHash ?? 0, (c, val) =>
        {
            var primary = PrimaryBiome;
            var biome = biomeDefsByShortHash.TryGetValue(val);
            if (biome == null && val != 0)
            {
                Log.Error("Did not find biome def with short hash " + val + " for cell " + c + ".");
                biomeDefsByShortHash.Add(val, primary);
            }
            biome ??= primary;
            array[map.cellIndices.CellToIndex(c)] = biome;
        }, name);
    }

    public enum BiomeQuery
    {
        Generic, PlantSpawning, AnimalSpawning
    }
}
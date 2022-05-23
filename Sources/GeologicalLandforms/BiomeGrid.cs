using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeologicalLandforms;

public class BiomeGrid : MapComponent
{
    private readonly BiomeDef[] _grid;

    private Dictionary<BiomeDef, int> _cellCounts = new();
    private BiomeDef _fallback = BiomeDefOf.TemperateForest;

    public IReadOnlyDictionary<BiomeDef, int> CellCounts => _cellCounts;
    public bool HasMultipleBiomes => CellCounts.Count > 1;
    public BiomeDef PrimaryBiome => _fallback;

    public BiomeGrid(Map map) : base(map)
    {
        _grid = new BiomeDef[map.cellIndices.NumGridCells];
    }

    public void Init(BiomeDef primaryBiome)
    {
        _fallback = primaryBiome ?? BiomeDefOf.TemperateForest;
        _cellCounts[_fallback] = map.cellIndices.NumGridCells;
    }

    public BiomeDef BiomeAt(int cell)
    {
        if (_grid == null) return _fallback;
        return _grid[cell] ?? _fallback;
    }
    
    public BiomeDef BiomeAt(IntVec3 c)
    {
        if (_grid == null) return _fallback;
        return _grid[map.cellIndices.CellToIndex(c)] ?? _fallback;
    }

    public void SetBiome(IntVec3 c, BiomeDef biomeDef)
    {
        int i = map.cellIndices.CellToIndex(c);
        BiomeDef old = BiomeAt(i);
        _cellCounts.TryGetValue(old, out var oldCount); 
        _cellCounts[old] = Math.Max(0, oldCount - 1);
        _grid[i] = biomeDef;
        _cellCounts.TryGetValue(biomeDef, out var newCount); 
        _cellCounts[biomeDef] = newCount + 1;
    }

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref _fallback, "fallback");
        var biomeDefsByShortHash = DefDatabase<BiomeDef>.AllDefs.ToDictionary(allDef => allDef.shortHash);
        ExposeBiomeArray(biomeDefsByShortHash, _grid, "biomeGrid");
        Scribe_Collections.Look(ref _cellCounts, "cellCounts", LookMode.Def, LookMode.Value);
    }

    private void ExposeBiomeArray(Dictionary<ushort, BiomeDef> biomeDefsByShortHash, BiomeDef[] array, string name)
    {
        MapExposeUtility.ExposeUshort(map, c => array[map.cellIndices.CellToIndex(c)]?.shortHash ?? 0, (c, val) =>
        {
            BiomeDef biome = biomeDefsByShortHash.TryGetValue(val);
            if (biome == null && val != 0)
            {
                Log.Error("Did not find biome def with short hash " + val + " for cell " + c + ".");
                biome = _fallback;
                biomeDefsByShortHash.Add(val, biome);
            }
            biome ??= _fallback;
            array[map.cellIndices.CellToIndex(c)] = biome;
        }, name);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class BiomeGrid : MapComponent
{
    private readonly BiomeDef[] _grid;

    private Dictionary<BiomeDef, int> _cellCounts = new();
    public IReadOnlyDictionary<BiomeDef, int> CellCounts => _cellCounts;
    public bool HasMultipleBiomes => CellCounts.Count > 1;

    private readonly IntVec3 _mapSize;
    
    private BiomeDef _primary;
    public BiomeDef PrimaryBiome
    {
        get
        {
            if (_primary != null) return _primary;
            MapParent parent = map?.info?.parent;
            WorldGrid worldGrid = Find.WorldGrid;
            if (parent == null || worldGrid == null) return BiomeDefOf.TemperateForest;
            Tile tile = worldGrid[parent.Tile];
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
    }

    public BiomeDef BiomeAt(int cell)
    {
        if (cell < 0 || cell >= _grid.Length) return PrimaryBiome;
        return _grid[cell] ?? PrimaryBiome;
    }
    
    public BiomeDef BiomeAt(IntVec3 c)
    {
        return BiomeAt(CellIndicesUtility.CellToIndex(c, _mapSize.x));
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

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref _primary, "primary");
        var biomeDefsByShortHash = DefDatabase<BiomeDef>.AllDefs.ToDictionary(allDef => allDef.shortHash);
        ExposeBiomeArray(biomeDefsByShortHash, _grid, "biomeGrid");
        Scribe_Collections.Look(ref _cellCounts, "cellCounts", LookMode.Def, LookMode.Value);
    }

    private void ExposeBiomeArray(Dictionary<ushort, BiomeDef> biomeDefsByShortHash, BiomeDef[] array, string name)
    {
        if (map == null) return;
        MapExposeUtility.ExposeUshort(map, c => array[map.cellIndices.CellToIndex(c)]?.shortHash ?? 0, (c, val) =>
        {
            BiomeDef primary = PrimaryBiome;
            BiomeDef biome = biomeDefsByShortHash.TryGetValue(val);
            if (biome == null && val != 0)
            {
                Log.Error("Did not find biome def with short hash " + val + " for cell " + c + ".");
                biomeDefsByShortHash.Add(val, primary);
            }
            biome ??= primary;
            array[map.cellIndices.CellToIndex(c)] = biome;
        }, name);
    }
}
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.TileEditor;

[HotSwappable]
public class TileEditorData
{
    // General
    public BiomeDef Biome;
    public Hilliness Hilliness;
    public float Pollution;
    public float Rainfall;
    public float Temperature;

    // Rock Types
    public Dictionary<ThingDef, float> RockTypes = [];

    // Features
    public List<TileMutatorDef> Features = [];

    public IEnumerable<Landform> Landforms => Features
        .Select(f => f.Worker is TileMutatorWorker_Landform worker ? worker.Landform : null)
        .Where(f => f != null);

    public void Read(PlanetTile tile)
    {
        ReadGeneral(tile);
        ReadRockTypes(tile);
        ReadFeatures(tile);
    }

    public void ReadGeneral(PlanetTile tile)
    {
        var tileObj = Find.World.grid.Surface[tile];

        Biome = tileObj.biome;
        Hilliness = tileObj.hilliness;
        Pollution = tileObj.pollution;
        Rainfall = tileObj.rainfall;
        Temperature = tileObj.temperature;
    }

    public void ReadRockTypes(PlanetTile tile)
    {
        if (Find.World.LandformData().TryGet(tile.tileId, out var data) && data.RockTypes != null)
        {
            RockTypes = new Dictionary<ThingDef, float>(data.RockTypes);
        }
        else
        {
            RockTypes = Find.World.NaturalRockTypesIn(tile).ToDictionary(e => e, _ => 0f);
        }
    }

    public void ReadFeatures(PlanetTile tile)
    {
        Features = Find.WorldGrid.Surface[tile].Mutators
            .Where(d => d.Worker is not TileMutatorWorker_Landform lf || lf.Landform?.WorldTileReq != null)
            .ToList();
    }

    public void ReadGenerated(PlanetTile tile)
    {
        ReadGeneral(tile);
        ReadGeneratedRockTypes(tile);
        ReadGeneratedFeatures(tile);
    }

    public void ReadGeneratedRockTypes(PlanetTile tile)
    {
        RockTypes = WorldTileUtils.OriginalRockTypesFor(tile).ToDictionary(e => e, _ => 0f);
    }

    public void ReadGeneratedFeatures(PlanetTile tile)
    {
        Features = TileMutatorsCustomization.BuildFresh(tile.tileId, Find.WorldGrid[tile].mutatorsNullable, false)
            .Where(d => d.Worker is not TileMutatorWorker_Landform lf || lf.Landform?.WorldTileReq != null)
            .ToList();
    }

    public static void Apply(PlanetTile tile, TileEditorData custom, TileEditorData generated)
    {
        var world = Find.World;
        var tileObj = world.grid.Surface[tile];

        tileObj.PrimaryBiome = custom.Biome;
        tileObj.hilliness = custom.Hilliness;
        tileObj.pollution = custom.Pollution;
        tileObj.rainfall = custom.Rainfall;
        tileObj.temperature = custom.Temperature;

        tileObj.hillinessLabelCached = null;
        tileObj.cachedMaxTemp = null;
        tileObj.cachedMinTemp = null;

        var hadSameAsGeneratedRockTypes = custom.EqualsRockTypes(generated);
        var hadSameAsGeneratedFeatures = custom.EqualsFeatures(generated);

        generated.ReadGenerated(tile);

        if (hadSameAsGeneratedRockTypes) custom.CopyRockTypes(generated);
        if (hadSameAsGeneratedFeatures) custom.CopyFeatures(generated);

        var worldData = world.LandformData();

        if (hadSameAsGeneratedRockTypes && hadSameAsGeneratedFeatures)
        {
            worldData.Reset(tile.tileId, false);
        }
        else
        {
            if (!worldData.TryGet(tile.tileId, out var tileData))
            {
                tileData = new LandformData.TileData(WorldTileInfo.Get(tile.tileId));
            }

            tileData.RockTypes = custom.RockTypes;

            tileData.Mutators = [];
            tileData.Landforms = [];

            foreach (var mutator in custom.Features)
            {
                if (mutator.Worker is TileMutatorWorker_Landform worker)
                {
                    tileData.Landforms.Add(worker.Landform.Id);
                }
                else
                {
                    tileData.Mutators.Add(mutator);
                }
            }

            worldData.Commit(tile.tileId, tileData, false);
        }

        TileMutatorsCustomization.TileHasChanged(tile.tileId);
        WorldTileInfo.InvalidateCache();
    }

    public void Copy(TileEditorData other)
    {
        CopyGeneral(other);
        CopyRockTypes(other);
        CopyFeatures(other);
    }

    public void CopyGeneral(TileEditorData other)
    {
        Biome = other.Biome;
        Hilliness = other.Hilliness;
        Pollution = other.Pollution;
        Rainfall = other.Rainfall;
        Temperature = other.Temperature;
    }

    public void CopyRockTypes(TileEditorData other)
    {
        RockTypes = new Dictionary<ThingDef, float>(other.RockTypes);
    }

    public void CopyFeatures(TileEditorData other)
    {
        Features = new List<TileMutatorDef>(other.Features);
    }

    public bool Equals(TileEditorData other) =>
        EqualsGeneral(other) &&
        EqualsRockTypes(other) &&
        EqualsFeatures(other);

    public bool EqualsGeneral(TileEditorData other) =>
        Biome == other.Biome &&
        Hilliness == other.Hilliness &&
        Pollution == other.Pollution &&
        Rainfall == other.Rainfall &&
        Temperature == other.Temperature;

    public bool EqualsRockTypes(TileEditorData other) =>
        RockTypes.Count == other.RockTypes.Count &&
        !RockTypes.Except(other.RockTypes).Any();

    public bool EqualsFeatures(TileEditorData other) =>
        Features.SequenceEqual(other.Features);
}

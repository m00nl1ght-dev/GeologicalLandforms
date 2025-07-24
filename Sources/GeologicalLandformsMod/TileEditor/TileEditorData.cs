using System.Collections.Generic;
using System.Linq;
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

    public void ReadOriginal(PlanetTile tile)
    {
        ReadOriginalGeneral(tile);
        ReadOriginalRockTypes(tile);
        ReadOriginalFeatures(tile);
    }

    public void ReadOriginalGeneral(PlanetTile tile)
    {
        // TODO
    }

    public void ReadOriginalRockTypes(PlanetTile tile)
    {
        RockTypes = WorldTileUtils.OriginalRockTypesFor(tile).ToDictionary(e => e, _ => 0f);
    }

    public void ReadOriginalFeatures(PlanetTile tile)
    {
        Features = TileMutatorsCustomization.BuildFresh(tile.tileId, Find.WorldGrid[tile].mutatorsNullable, false)
            .Where(d => d.Worker is not TileMutatorWorker_Landform lf || lf.Landform?.WorldTileReq != null)
            .ToList();
    }

    public void Apply(PlanetTile tile, TileEditorData original)
    {
        var world = Find.World;
        var tileObj = world.grid.Surface[tile];

        tileObj.PrimaryBiome = Biome;
        tileObj.hilliness = Hilliness;
        tileObj.pollution = Pollution;
        tileObj.rainfall = Rainfall;
        tileObj.temperature = Temperature;

        var worldData = world.LandformData();

        if (EqualsRockTypes(original) && EqualsFeatures(original))
        {
            worldData.Reset(tile.tileId, false);
        }
        else
        {
            if (!worldData.TryGet(tile.tileId, out var tileData))
            {
                tileData = new LandformData.TileData(WorldTileInfo.Get(tile.tileId));
            }

            tileData.RockTypes = RockTypes;

            tileData.Mutators = [];
            tileData.Landforms = [];

            foreach (var mutator in Features)
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

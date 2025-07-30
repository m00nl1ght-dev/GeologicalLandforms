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
    // TODO add elevation (need to change it when switching biome between ocean / not ocean

    // Rock Types
    public Dictionary<ThingDef, float> RockTypes = [];

    // Features
    public List<TileMutatorDef> Features = [];

    // Feature Config
    public Dictionary<string, TileFeatureProperties> FeatureProperties = [];

    public IEnumerable<Landform> Landforms => Features.Select(f => f.AsLandform()).WhereNotNull();

    public void Read(PlanetTile tile)
    {
        ReadGeneral(tile);
        ReadRockTypes(tile);
        ReadFeatures(tile);
        ReadFeatureProperties(tile);
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
        Features = Find.WorldGrid.Surface[tile].Mutators.Where(d => d.AsLandform() is not { WorldTileReq: null }).ToList();
    }

    public void ReadFeatureProperties(PlanetTile tile)
    {
        FeatureProperties = Find.World.LandformData().TryGet(tile.tileId, out var data) ? data.FeatureProperties ?? [] : [];
    }

    public void ReadGenerated(PlanetTile tile)
    {
        ReadGeneral(tile);
        ReadGeneratedRockTypes(tile);
        ReadGeneratedFeatures(tile);
        ReadGeneratedFeatureProperties(tile);
    }

    public void ReadGeneratedRockTypes(PlanetTile tile)
    {
        RockTypes = WorldTileUtils.OriginalRockTypesFor(tile).ToDictionary(e => e, _ => 0f);
    }

    public void ReadGeneratedFeatures(PlanetTile tile)
    {
        Features = TileMutatorsCustomization.BuildFresh(tile.tileId, Find.WorldGrid[tile].mutatorsNullable, false)
            .Where(d => d.AsLandform() is not { WorldTileReq: null })
            .ToList();
    }

    public void ReadGeneratedFeatureProperties(PlanetTile tile)
    {
        FeatureProperties = [];
    }

    public bool TryGetFeatureProperty(TileMutatorDef feature, string propertyId, out string value)
    {
        if (FeatureProperties.TryGetValue(feature.defName, out var properties))
            if (properties.Properties.TryGetValue(propertyId, out value))
                return true;

        value = null;
        return false;
    }

    public void SetFeatureProperty(TileMutatorDef feature, string propertyId, string value)
    {
        if (FeatureProperties.TryGetValue(feature.defName, out var properties))
        {
            properties.Properties[propertyId] = value;
        }
        else
        {
            FeatureProperties[feature.defName] = new() { Properties = { [propertyId] = value } };
        }
    }

    public void ClearFeatureProperty(TileMutatorDef feature, string propertyId)
    {
        if (FeatureProperties.TryGetValue(feature.defName, out var properties))
        {
            properties.Properties.Remove(propertyId);
        }
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

        if (hadSameAsGeneratedRockTypes && hadSameAsGeneratedFeatures && custom.FeatureProperties.Count == 0)
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
            tileData.FeatureProperties = custom.FeatureProperties;

            tileData.Mutators = [];
            tileData.Landforms = [];

            foreach (var mutator in custom.Features)
            {
                if (mutator.AsLandform() is {} landform)
                {
                    tileData.Landforms.Add(landform.Id);

                    // TODO set cave system depth if the landform has it as a requirement
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
        CopyFeatureProperties(other);
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

    public void CopyFeatureProperties(TileEditorData other)
    {
        FeatureProperties = other.FeatureProperties.ToDictionary(e => e.Key, e => new TileFeatureProperties(e.Value));
    }

    public bool Equals(TileEditorData other) =>
        EqualsGeneral(other) &&
        EqualsRockTypes(other) &&
        EqualsFeatures(other) &&
        EqualsFeatureProperties(other);

    public bool EqualsGeneral(TileEditorData other) =>
        Biome == other.Biome &&
        Hilliness == other.Hilliness &&
        Pollution == other.Pollution &&
        Rainfall == other.Rainfall &&
        Temperature == other.Temperature;

    public bool EqualsRockTypes(TileEditorData other) =>
        RockTypes.DictEquals(other.RockTypes);

    public bool EqualsFeatures(TileEditorData other) =>
        Features.SequenceEqual(other.Features);

    public bool EqualsFeatureProperties(TileEditorData other) =>
        FeatureProperties.DictEquals(other.FeatureProperties);
}

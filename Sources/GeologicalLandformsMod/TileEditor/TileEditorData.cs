using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.TileEditor;

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

    public void Read(PlanetTile tile)
    {
        ReadGeneral(tile);
        ReadRockTypes(tile);
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

    public void Reset(PlanetTile tile)
    {
        ResetGeneral(tile);
        ResetRockTypes(tile);
    }

    public void ResetGeneral(PlanetTile tile)
    {
        // TODO
    }

    public void ResetRockTypes(PlanetTile tile)
    {
        RockTypes = WorldTileUtils.OriginalRockTypesFor(tile).ToDictionary(e => e, _ => 0f);
    }

    public void Apply(PlanetTile tile)
    {
        var world = Find.World;
        var tileObj = world.grid.Surface[tile];

        tileObj.PrimaryBiome = Biome;
        tileObj.hilliness = Hilliness;
        tileObj.pollution = Pollution;
        tileObj.rainfall = Rainfall;
        tileObj.temperature = Temperature;

        var worldData = world.LandformData();

        if (!worldData.TryGet(tile.tileId, out var tileData))
            tileData = new LandformData.TileData(WorldTileInfo.Get(tile.tileId));

        tileData.RockTypes = RockTypes;

        worldData.Commit(tile.tileId, tileData, false);
        WorldTileInfo.InvalidateCache();
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms;

public class BiomeProperties : DefModExtension
{
    public bool isWaterCovered;
    
    public bool allowLandforms = true;
    public bool allowBiomeTransitions = true;
    
    public List<string> disallowedLandforms;
    public List<BiomeDef> disallowedBiomeTransitions;

    public TerrainDef beachTerrain;
    public TerrainDef gravelTerrain;
    
    public BiomeProperties() {}

    public BiomeProperties(BiomeProperties other)
    {
        isWaterCovered = other.isWaterCovered;
        allowLandforms = other.allowLandforms;
        allowBiomeTransitions = other.allowBiomeTransitions;
        disallowedLandforms = other.disallowedLandforms?.ToList();
        disallowedBiomeTransitions = other.disallowedBiomeTransitions?.ToList();
        beachTerrain = other.beachTerrain;
        gravelTerrain = other.gravelTerrain;
    }

    public static BiomeProperties[] GetAll()
    {
        var defaultsByDefName = DefaultsByDefName;
        var defaultsByPackageId = DefaultsByPackageId;
        var all = new BiomeProperties[DefDatabase<BiomeDef>.DefCount];
        
        foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefs)
        {
            var properties = biomeDef.GetModExtension<BiomeProperties>();
            properties ??= defaultsByDefName.TryGetValue(biomeDef.defName);
            properties ??= defaultsByPackageId.TryGetValue(biomeDef.modContentPack.ModMetaData.PackageIdNonUnique);
            properties ??= new BiomeProperties();
            all[biomeDef.index] = properties;
        }

        return all;
    }

    private static Dictionary<string, BiomeProperties> DefaultsByPackageId => new()
    {
        {
            "biomesteam.biomesislands", new BiomeProperties
            {
                isWaterCovered = true,
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "biomesteam.biomescaverns", new BiomeProperties
            {
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "biomesteam.oasis", new BiomeProperties
            {
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "mlie.terraprojectcore", new BiomeProperties
            {
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "mlie.cavebiome", new BiomeProperties
            {
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "sindre0830.RimNauts2", new BiomeProperties
            {
                isWaterCovered = true,
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        }
    };
    
    private static Dictionary<string, BiomeProperties> DefaultsByDefName => new()
    {
        {
            "Ocean", new BiomeProperties
            {
                isWaterCovered = true,
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "Lake", new BiomeProperties
            {
                isWaterCovered = true,
                allowLandforms = false,
                allowBiomeTransitions = false
            }
        },
        {
            "SeaIce", new BiomeProperties
            {
                beachTerrain = TerrainDefOf.Ice
            }
        }
    };
}
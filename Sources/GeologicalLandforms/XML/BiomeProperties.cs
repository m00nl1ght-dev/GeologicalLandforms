using System.Collections.Generic;
using RimWorld;
using Verse;

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

    public static BiomeProperties[] GetAll()
    {
        var all = new BiomeProperties[DefDatabase<BiomeDef>.DefCount];
        
        foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefs)
        {
            var properties = biomeDef.GetModExtension<BiomeProperties>();
            properties ??= DefaultsByDefName.TryGetValue(biomeDef.defName);
            properties ??= DefaultsByPackageId.TryGetValue(biomeDef.modContentPack.ModMetaData.PackageIdNonUnique);
            properties ??= new BiomeProperties();
            all[biomeDef.index] = properties;
        }

        return all;
    }

    private static readonly Dictionary<string, BiomeProperties> DefaultsByPackageId = new()
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
        }
    };
    
    private static readonly Dictionary<string, BiomeProperties> DefaultsByDefName = new()
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
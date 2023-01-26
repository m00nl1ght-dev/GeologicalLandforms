using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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

    [Unsaved] public bool allowLandformsByUser = true;
    [Unsaved] public bool allowBiomeTransitionsByUser = true;

    public bool AllowLandforms => allowLandforms && allowLandformsByUser;
    public bool AllowBiomeTransitions => allowBiomeTransitions && allowBiomeTransitionsByUser;
    
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

    private static BiomeProperties[] _cache;

    public static BiomeProperties Get(BiomeDef biomeDef)
    {
        try
        {
            return GeologicalLandformsAPI.ApplyBiomePropertiesHook(biomeDef, _cache[biomeDef.index]);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return new BiomeProperties();
        }
    }

    public static void RebuildCache()
    {
        var defaultsByDefName = DefaultsByDefName;
        var defaultsByPackageId = DefaultsByPackageId;
        var all = new BiomeProperties[DefDatabase<BiomeDef>.DefCount];
        
        foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefs)
        {
            var properties = biomeDef.GetModExtension<BiomeProperties>();
            properties ??= defaultsByDefName.TryGetValue(biomeDef.defName);
            properties ??= defaultsByPackageId.TryGetValue(biomeDef.modContentPack?.ModMetaData?.PackageIdNonUnique ?? "");
            properties ??= IsSpecialPurposeBiome(biomeDef) ? DefaultsForSpecialPurposeBiome : DefaultsForStandardBiome;
            all[biomeDef.index] = new BiomeProperties(properties);
        }

        _cache = all;
    }
    
    private static readonly BiomeProperties DefaultsForStandardBiome = new();

    private static readonly BiomeProperties DefaultsForSpecialPurposeBiome = new()
    {
        allowLandforms = false,
        allowBiomeTransitions = false
    };

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

    private static bool IsSpecialPurposeBiome(BiomeDef biome)
    {
        return false; // TODO
    }
}
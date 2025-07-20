using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.XML;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class BiomeProperties : DefModExtension
{
    public bool allowLandforms = true;
    public bool allowBiomeTransitions = true;

    public List<string> allowedLandforms;
    public List<string> disallowedLandforms;

    public List<BiomeDef> allowedBiomeTransitions;
    public List<BiomeDef> disallowedBiomeTransitions;

    public XmlDynamicValue<List<string>, ICtxTile> overrideLandforms;
    public XmlDynamicValue<List<string>, ICtxTile> overrideBiomeVariants;

    public bool isWaterCovered;
    public bool hasStableCaveRoofs;
    public bool allowSettlementsOnImpassableTerrain;

    public WorldTileOverrides worldTileOverrides;
    public WorldTileGraphicAtlas worldTileGraphicAtlas;

    #if !RW_1_6_OR_GREATER
    public bool applyToCaves;
    public TerrainDef beachTerrain;
    public TerrainDef gravelTerrain;
    #endif

    public List<BiomeVariantLayer> biomeLayers;

    [Unsaved] public bool allowLandformsByUser = true;
    [Unsaved] public bool allowBiomeTransitionsByUser = true;

    public bool AllowLandforms => allowLandforms && allowLandformsByUser;
    public bool AllowBiomeTransitions => allowBiomeTransitions && allowBiomeTransitionsByUser;

    public bool AllowsLandform(Landform landform)
    {
        if (allowedLandforms != null) return allowedLandforms.Contains(landform.Id);
        if (!AllowLandforms && (!landform.IsLayer || !landform.IsInternal || !AllowBiomeTransitions)) return false;
        return !(disallowedLandforms?.Contains(landform.Id) ?? false);
    }

    public bool AllowsBiomeTransition(BiomeDef biome)
    {
        if (allowedBiomeTransitions != null) return allowedBiomeTransitions.Contains(biome);
        if (!AllowBiomeTransitions) return false;
        return !(disallowedBiomeTransitions?.Contains(biome) ?? false);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        if (biomeLayers != null)
        {
            foreach (var layer in biomeLayers)
            {
                if (string.IsNullOrEmpty(layer.id) || layer.id == "main") yield return "layer id must be set";
                else if (!BiomeVariantLayer.AllowedIdRegex.IsMatch(layer.id)) yield return $"layer id {layer.id} in invalid";
                else if (biomeLayers.Any(l => l.id == layer.id && l != layer)) yield return $"layer id duplicate: {layer}";
            }
        }
    }

    public class WorldTileOverrides
    {
        public XmlDynamicValue<float, ICtxTile> elevation;
        public XmlDynamicValue<float, ICtxTile> temperature;
        public XmlDynamicValue<float, ICtxTile> rainfall;
        public XmlDynamicValue<float, ICtxTile> pollution;

        public XmlDynamicValue<List<ThingDef>, ICtxTile> stoneTypes;
    }

    public static bool AnyHasTileGraphic { get; private set; }

    private static BiomeProperties[] _cache;

    public static BiomeProperties Get(BiomeDef biomeDef)
    {
        try
        {
            return _cache[biomeDef.index];
        }
        catch (ThreadAbortException) { throw; }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Error($"Error getting biome properties for {biomeDef?.defName}\n{e}");
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
            all[biomeDef.index] = properties.Copy();
        }

        AnyHasTileGraphic = all.Any(p => p.worldTileGraphicAtlas != null);
        _cache = all;
    }

    public BiomeProperties Copy()
    {
        var copy = (BiomeProperties) MemberwiseClone();
        copy.allowedLandforms = allowedLandforms?.ToList();
        copy.disallowedLandforms = disallowedLandforms?.ToList();
        copy.allowedBiomeTransitions = allowedBiomeTransitions?.ToList();
        copy.disallowedBiomeTransitions = disallowedBiomeTransitions?.ToList();
        return copy;
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
            "mlie.rfarchipelagos", new BiomeProperties
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
                #if !RW_1_6_OR_GREATER
                beachTerrain = TerrainDefOf.Ice
                #endif
            }
        },
        {
            "Glowforest", new BiomeProperties
            {
                allowBiomeTransitions = false
            }
        }
    };

    private static readonly List<OpCode> _extAccessOps = [OpCodes.Call, OpCodes.Callvirt, OpCodes.Ldfld];

    private static bool IsSpecialPurposeBiome(BiomeDef biome)
    {
        #if RW_1_5_OR_GREATER
        if (!biome.generatesNaturally) return true;
        #endif

        if (biome.modContentPack is { IsOfficialMod: true }) return false;
        if (!biome.implemented || biome.workerClass == null) return true;
        if (biome.workerClass.Name == "UniversalBiomeWorker") return false;

        try
        {
            #if RW_1_6_OR_GREATER
            var method = AccessTools.Method(biome.workerClass, nameof(BiomeWorker.GetScore), [typeof(BiomeDef), typeof(Tile), typeof(PlanetTile)]);
            #else
            var method = AccessTools.Method(biome.workerClass, nameof(BiomeWorker.GetScore), [typeof(Tile), typeof(int)]);
            #endif

            if (method == null) throw new Exception("method GetScore not found");

            var instructions = PatchProcessor.ReadMethodBody(method);
            if (instructions.All(op => !_extAccessOps.Contains(op.Key)))
            {
                GeologicalLandformsAPI.Logger.Debug($"Detected special-purpose biome {biome.defName} in {biome.modContentPack?.Name}");
                return true;
            }
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Warn($"Failed to check if {biome.defName} is a special-purpose biome", e);
        }

        return false;
    }
}

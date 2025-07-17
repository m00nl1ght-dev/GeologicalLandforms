using System;
using System.Collections.Generic;
using System.Threading;
using LunarFramework.XML;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public class BiomeWorkerConfig : DefModExtension
{
    public XmlDynamicValue<float, ICtxEarlyTile> score;

    public static void ApplyAll()
    {
        foreach (var def in DefDatabase<BiomeDef>.AllDefs)
        {
            if (def.workerClass == typeof(ConfigurableBiomeWorker))
            {
                var biomeWorker = (ConfigurableBiomeWorker) def.Worker;
                var config = def.GetModExtension<BiomeWorkerConfig>();

                if (config != null)
                {
                    #if RW_1_6_OR_GREATER
                    biomeWorker.ScoreSuppliers[def] = config.score;
                    #else
                    biomeWorker.ScoreSupplier = config.score;
                    #endif
                }
                else
                {
                    GeologicalLandformsAPI.Logger.Warn($"Biome {def.defName} has no BiomeWorkerConfig");
                }
            }
        }
    }
}

public class ConfigurableBiomeWorker : BiomeWorker
{
    #if RW_1_6_OR_GREATER
    internal readonly Dictionary<BiomeDef, XmlDynamicValue<float, ICtxEarlyTile>> ScoreSuppliers = new();
    #else
    internal XmlDynamicValue<float, ICtxEarlyTile> ScoreSupplier = new();
    #endif

    private int _erroredWorldHash = -1;

    #if RW_1_6_OR_GREATER
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
    #else
    public override float GetScore(Tile tile, int tileID)
    #endif
    {
        try
        {
            #if RW_1_6_OR_GREATER
            return ScoreSuppliers[biome].Get(new CtxEarlyTile(planetTile.tileId, tile, Find.World));
            #else
            return ScoreSupplier.Get(new CtxEarlyTile(tileID, tile, Find.World));
            #endif
        }
        catch (ThreadAbortException) { throw; }
        catch (Exception e)
        {
            var worldHash = Find.World.GetHashCode();

            if (_erroredWorldHash != worldHash)
            {
                _erroredWorldHash = worldHash;
                GeologicalLandformsAPI.Logger.Error("Error in configurable biome worker", e);
            }

            return -999f;
        }
    }
}

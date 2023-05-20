using System;
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
                    biomeWorker.ScoreSupplier = config.score;
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
    internal XmlDynamicValue<float, ICtxEarlyTile> ScoreSupplier = new();

    private int _erroredWorldHash = -1;

    public override float GetScore(Tile tile, int tileID)
    {
        try
        {
            return ScoreSupplier.Get(new CtxEarlyTile(tileID, tile, Find.World));
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

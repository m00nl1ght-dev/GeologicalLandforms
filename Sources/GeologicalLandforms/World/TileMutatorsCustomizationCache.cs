#if RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public static class TileMutatorsCustomizationCache
{
    private static IList<TileMutatorDef>[] _cache;

    private static List<TileMutatorDef> _disabledMutators = [];
    private static List<LandmarkDef> _disabledLandmarks = [];

    public static bool IsTileMutatorDisabled(TileMutatorDef def)
    {
        return _disabledMutators.Contains(def);
    }

    public static bool IsLandmarkDisabled(LandmarkDef def)
    {
        return _disabledLandmarks.Contains(def);
    }

    public static IList<TileMutatorDef> Get(int tileId, IList<TileMutatorDef> original)
    {
        _cache ??= new IList<TileMutatorDef>[Find.WorldGrid.TilesCount];

        var fromCache = _cache[tileId];
        if (fromCache != null) return fromCache;

        var fresh = BuildFresh(tileId, original);
        _cache[tileId] = fresh;
        return fresh;
    }

    public static IList<TileMutatorDef> BuildFresh(int tileId, IList<TileMutatorDef> original)
    {
        var tileInfo = WorldTileInfo.Get(tileId);

        List<TileMutatorDef> mutators = null;

        if (original != null)
        {
            foreach (var tileMutatorDef in original)
            {
                if (!IsTileMutatorDisabled(tileMutatorDef))
                {
                    mutators ??= [];
                    mutators.Add(tileMutatorDef);
                }
            }
        }

        if (tileInfo.Landforms != null)
        {
            mutators ??= [];

            foreach (var landform in tileInfo.Landforms)
            {
                mutators.Add(landform.TileMutatorDef);

                if (landform.OutputElevation?.InputKnob.connected() == true)
                {
                    mutators.Remove(TileMutatorDefOf.Mountain);
                }

                if (landform.OutputTerrain?.BaseKnob.connected() == true &&
                    landform.WorldTileReq?.Topology.IsCoast(true) == true)
                {
                    mutators.Remove(TileMutatorDefOf.Coast);
                    mutators.Remove(TileMutatorDefOf.Lakeshore);
                }

                if (landform.OutputWaterFlow?.RiverTerrainKnob.connected() == true)
                {
                    mutators.Remove(TileMutatorDefOf.River);
                    mutators.Remove(TileMutatorDefOf.RiverConfluence);
                    mutators.Remove(TileMutatorDefOf.RiverDelta);
                    mutators.Remove(TileMutatorDefOf.RiverIsland);
                    mutators.Remove(TileMutatorDefOf.Headwater);
                }

                if (landform.OutputBiomeGrid?.BiomeGridKnob.connected() == true ||
                    landform.OutputBiomeGrid?.BiomeTransitionKnob.connected() == true)
                {
                    mutators.Remove(TileMutatorDefOf.MixedBiome);
                }

                if (landform.OutputCaves?.InputKnob.connected() == true)
                {
                    mutators.Remove(TileMutatorDefOf.Caves);
                }
                else if (!landform.IsLayer && landform.WorldTileReq != null)
                {
                    if (Rand.ChanceSeeded(landform.WorldTileReq.CaveChance, tileInfo.StableSeed(8266)))
                    {
                        mutators.AddUnique(TileMutatorDefOf.Caves);
                    }
                    else
                    {
                        mutators.Remove(TileMutatorDefOf.Caves);
                    }
                }
            }
        }

        return mutators != null ? mutators : Array.Empty<TileMutatorDef>();
    }

    public static void RefreshCustomization()
    {
        _disabledMutators = DefDatabase<TileMutatorDef>.AllDefs
            .Where(m => !GeologicalLandformsAPI.TileMutatorEnabled.Apply(m))
            .ToList();

        _disabledLandmarks = DefDatabase<LandmarkDef>.AllDefs
            .Where(lm => lm.mutatorChances.Where(m => m.required).Any(m => _disabledMutators.Contains(m.mutator)))
            .ToList();

        Clear();

        Find.World?.grid.Surface.SetDirty<WorldDrawLayer_Landmarks>();
        ExpandableLandmarksUtility.Notify_WorldObjectsChanged();
    }

    public static void Clear(int tileId)
    {
        if (_cache != null)
            _cache[tileId] = null;
    }

    public static void Clear()
    {
        _cache = null;
    }

    public static readonly IReadOnlyDictionary<string, string[]> Exclusions = new Dictionary<string, string[]> {
        { "Cliff", [ "Mountain" ] },
        { "River", [ "River" ] },
        { "RiverDelta", [ "RiverDelta" ] },
        { "RiverSource", [ "Headwater" ] },
        { "RiverIsland", [ "RiverIsland" ] },
        { "RiverConfluence", [ "RiverConfluence" ] },
        { "BiomeTransitions", [ "MixedBiome" ] },
    };

    public static readonly IReadOnlyDictionary<string, string> MadeObsoleteBy = new Dictionary<string, string> {
        { "Archipelago", "Archipelago" },
        { "Cove", "Cove" },
        { "CliffCorner", "Cliffs" },
        { "CoastalIsland", "CoastalIsland" },
        { "DryLake", "DryLake" },
        { "Fjord", "Fjord" },
        { "Lake", "Lake" },
        { "LakeWithIsland", "LakeWithIsland" },
        { "Oasis", "Oasis" },
        { "Peninsula", "Peninsula" },
        { "Valley", "Valley" },
    };
}

#endif

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

internal static class TerrainTabUI
{
    internal static void DoTerrainTabUI(Listing_Standard listing)
    {
        int tileId = Find.WorldSelector.selectedTile;
        var worldTileInfo = WorldTileInfo.Get(tileId);

        var rect = listing.GetRect(28f);
        if (!listing.BoundingRectCached.HasValue || rect.Overlaps(listing.BoundingRectCached.Value))
        {
            if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.FindLandform".Translate()))
            {
                var options = new List<FloatMenuOption>
                {
                    new("GeologicalLandforms.WorldMap.FindLandformAny".Translate(), () => FindLandform(null)),
                    new("GeologicalLandforms.WorldMap.FindLandformAnyPOI".Translate(), () => FindLandform(null, true))
                };
                options.AddRange(LandformManager.Landforms.Values
                    .Where(e => !e.IsLayer && GeologicalLandformsMod.IsLandformEnabled(e))
                    .OrderBy(e => e.TranslatedNameForSelection)
                    .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () => FindLandform(e))));
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        listing.Gap();

        if (GeologicalLandformsMod.Settings.EnableGodMode)
        {
            var landformData = Find.World.LandformData();
            bool ignoreReq = Prefs.DevMode && GeologicalLandformsMod.Settings.IgnoreWorldTileReqInGodMode;

            if (landformData != null && !landformData.IsLocked(tileId))
            {
                rect = listing.GetRect(28f);

                if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.SetLandform".Translate()))
                {
                    var biomeProps = worldTileInfo.Biome.Properties();

                    var eligible = LandformManager.Landforms.Values
                        .Where(e => ignoreReq || worldTileInfo.CanHaveLandform(e, biomeProps, true))
                        .Where(e => !e.IsLayer && GeologicalLandformsMod.IsLandformEnabled(e))
                        .ToList();

                    var options = new List<FloatMenuOption>
                    {
                        new("GeologicalLandforms.WorldMap.SetLandformAuto".Translate(), () =>
                        {
                            landformData.Reset(tileId);
                        }),
                        new("None".Translate(), () =>
                        {
                            landformData.Commit(tileId, null, worldTileInfo.TopologyDirection);
                        })
                    };

                    options.AddRange(eligible
                        .OrderBy(e => e.TranslatedNameForSelection)
                        .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () =>
                        {
                            if (ignoreReq)
                            {
                                var dirOptions = new List<FloatMenuOption>(new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
                                    .Select(d => new FloatMenuOption(e.TranslatedDirection(d).CapitalizeFirst(), () =>
                                    {
                                        landformData.Commit(tileId, e, d);
                                    })));
                                Find.WindowStack.Add(new FloatMenu(dirOptions) { vanishIfMouseDistant = false });
                            }
                            else
                            {
                                landformData.Commit(tileId, e, worldTileInfo.TopologyDirection);
                            }
                        })));
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            listing.Gap();
        }

        if (Prefs.DevMode && GeologicalLandformsMod.Settings.ShowWorldTileDebugInfo)
        {
            listing.LabelDouble("GeologicalLandforms.WorldMap.Topology".Translate(), worldTileInfo.Topology.ToString());
            listing.LabelDouble("GeologicalLandforms.WorldMap.TopologyDirection".Translate(), worldTileInfo.TopologyDirection.ToStringHuman());
            listing.LabelDouble("GeologicalLandforms.WorldMap.DepthInCaveSystem".Translate(), worldTileInfo.DepthInCaveSystem.ToString());
            listing.LabelDouble("GeologicalLandforms.WorldMap.Swampiness".Translate(), worldTileInfo.Swampiness.ToString(CultureInfo.InvariantCulture));
            listing.LabelDouble("GeologicalLandforms.WorldMap.RiverAngle".Translate(), worldTileInfo.MainRiverAngle.ToString(CultureInfo.InvariantCulture));
            listing.LabelDouble("GeologicalLandforms.WorldMap.MainRoadAngle".Translate(), worldTileInfo.MainRoadAngle.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void FindLandform(Landform landform, bool requirePoi = false)
    {
        int tileId = Find.WorldSelector.selectedTile;
        var grid = Find.WorldGrid;

        HashSet<int> tested = new();
        HashSet<int> pending = new() { tileId };

        var nbData = grid.tileIDToNeighbors_values;
        var nbOffsets = grid.tileIDToNeighbors_offsets;

        bool Matches(IWorldTileInfo tileInfo)
        {
            if (tileInfo.Landforms == null) return false;
            if (landform != null) return tileInfo.Landforms.Contains(landform);
            return tileInfo.Landforms.Any(l => !l.IsLayer && (!requirePoi || l.IsPointOfInterest()));
        }

        var maxDistance = GeologicalLandformsMod.Settings.MaxLandformSearchRadius.Value;

        for (int i = 0; i < maxDistance; i++)
        {
            var copy = pending.ToList();
            pending.Clear();
            foreach (var p in copy)
            {
                IWorldTileInfo tileInfo = WorldTileInfo.Get(p);
                if (Matches(tileInfo))
                {
                    CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(p));
                    Find.WorldSelector.selectedTile = p;
                    float dist = grid.ApproxDistanceInTiles(tileId, p);
                    var messageSuccess = "GeologicalLandforms.WorldMap.FindLandformSuccess"
                        .Translate(landform.TranslatedNameForSelection, dist.ToString("F2"));
                    Messages.Message(messageSuccess, MessageTypeDefOf.SilentInput, false);
                    return;
                }

                tested.Add(p);

                var nbOffset = nbOffsets[p];
                var nbBound = nbOffsets.IdxBoundFor(nbData, p);

                for (int j = nbOffset; j < nbBound; j++)
                {
                    var nTile = nbData[j];
                    if (tested.Contains(nTile)) continue;
                    pending.Add(nTile);
                }
            }
        }

        var messageFail = "GeologicalLandforms.WorldMap.FindLandformFail"
            .Translate(landform.TranslatedNameForSelection, maxDistance.ToString("F0"));
        Messages.Message(messageFail, MessageTypeDefOf.RejectInput, false);
    }
}

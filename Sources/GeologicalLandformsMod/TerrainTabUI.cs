using System;
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
        var tileInfo = WorldTileInfo.Get(tileId);

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
                    .Where(e => !e.IsLayer && !e.IsInternal && GeologicalLandformsMod.IsLandformEnabled(e))
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

            if (landformData != null && tileInfo.WorldObject is not { HasMap: true })
            {
                rect = listing.GetRect(28f);

                if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.SetLandform".Translate()))
                {
                    var biomeProps = tileInfo.Biome.Properties();

                    var eligible = LandformManager.Landforms.Values
                        .Where(e => ignoreReq || tileInfo.CanHaveLandform(e, biomeProps, true))
                        .Where(e => !e.IsLayer && (ignoreReq || !e.IsInternal) && GeologicalLandformsMod.IsLandformEnabled(e))
                        .ToList();

                    var options = new List<FloatMenuOption>
                    {
                        new("GeologicalLandforms.WorldMap.SetLandformAuto".Translate(), () =>
                        {
                            landformData.Reset(tileId);
                        }),
                        new("None".Translate(), () =>
                        {
                            landformData.Commit(tileId, new LandformData.TileData(tileInfo)
                            {
                                Landforms = tileInfo.Landforms?.Where(lf => lf.IsLayer).Select(lf => lf.Id).ToList()
                            });
                        })
                    };

                    options.AddRange(eligible
                        .OrderBy(e => e.TranslatedNameForSelection)
                        .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () =>
                        {
                            var layers = tileInfo.Landforms?.Where(lf => lf.IsLayer) ?? new List<Landform>();
                            
                            if (ignoreReq)
                            {
                                var dirOptions = new List<FloatMenuOption>(new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
                                    .Select(dir => new FloatMenuOption(e.TranslatedDirection(dir).CapitalizeFirst(), () =>
                                    {
                                        CoerceTileToRequirements(tileInfo, landformData, e);
                                        landformData.Commit(tileId, new LandformData.TileData(tileInfo)
                                        {
                                            Landforms = layers.Append(e).Select(lf => lf.Id).ToList(),
                                            TopologyDirection = dir,
                                            TopologyValue = 0f
                                        });
                                    })));
                                Find.WindowStack.Add(new FloatMenu(dirOptions) { vanishIfMouseDistant = false });
                            }
                            else
                            {
                                landformData.Commit(tileId, new LandformData.TileData(tileInfo)
                                {
                                    Landforms = layers.Append(e).Select(lf => lf.Id).ToList(),
                                    TopologyValue = 0f
                                });
                            }
                        })));
                    
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            listing.Gap();
        }

        if (Prefs.DevMode && GeologicalLandformsMod.Settings.ShowWorldTileDebugInfo)
        {
            listing.LabelDouble("GeologicalLandforms.WorldMap.Topology".Translate(), tileInfo.Topology.ToString());
            listing.LabelDouble("GeologicalLandforms.WorldMap.TopologyDirection".Translate(), tileInfo.TopologyDirection.ToStringHuman());
            listing.LabelDouble("GeologicalLandforms.WorldMap.DepthInCaveSystem".Translate(), tileInfo.DepthInCaveSystem.ToString());
            listing.LabelDouble("GeologicalLandforms.WorldMap.Swampiness".Translate(), tileInfo.Swampiness.ToString(CultureInfo.InvariantCulture));
            listing.LabelDouble("GeologicalLandforms.WorldMap.RiverAngle".Translate(), tileInfo.MainRiverAngle.ToString(CultureInfo.InvariantCulture));
            listing.LabelDouble("GeologicalLandforms.WorldMap.MainRoadAngle".Translate(), tileInfo.MainRoadAngle.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void CoerceTileToRequirements(WorldTileInfo tile, LandformData data, Landform landform)
    {
        if (landform.WorldTileReq != null)
        {
            if (landform.WorldTileReq.HillinessRequirement.min > 5f)
            {
                tile.Tile.hilliness = Hilliness.Impassable;
            }
            else if (tile.Tile.hilliness == Hilliness.Impassable && landform.WorldTileReq.HillinessRequirement.max <= 5f)
            {
                tile.Tile.hilliness = (Hilliness) landform.WorldTileReq.HillinessRequirement.max;
            }
            
            if (landform.WorldTileReq.DepthInCaveSystemRequirement.min >= 1)
            {
                data.SetCaveSystemDepthAt(tile.TileId, (byte) (int) landform.WorldTileReq.DepthInCaveSystemRequirement.min);
            }
            else if (landform.WorldTileReq.Topology is Topology.CaveEntrance or Topology.CaveTunnel)
            {
                data.SetCaveSystemDepthAt(tile.TileId, 1);
            }
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

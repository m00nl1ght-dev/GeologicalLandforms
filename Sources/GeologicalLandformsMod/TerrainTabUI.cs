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
        if (tileId < 0) return;

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

                #if RW_1_6_OR_GREATER

                var prefix = "GeologicalLandforms.WorldMap.FindLandformPrefix".Translate();

                options.AddRange(LandformManager.LandformsById.Values
                    .Where(e => !e.IsInternal && GeologicalLandformsMod.IsLandformEnabled(e))
                    .OrderBy(e => e.TranslatedNameForSelection)
                    .Select(e => new FloatMenuOption($"{prefix} {e.TranslatedNameForSelection.CapitalizeFirst()}", () => FindLandform(e))));

                options.AddRange(DefDatabase<TileMutatorDef>.AllDefs
                    .Where(e => e.Worker is not TileMutatorWorker_Landform && !TileMutatorsCustomization.IsTileMutatorDisabled(e))
                    .Where(e => !GeologicalLandformsSettings.SpecialTileMutatorsHidden.Contains(e.defName))
                    .OrderBy(e => e.modContentPack.ContentSourceLabel()).ThenBy(e => e.label)
                    .Select(e => new FloatMenuOption($"({e.modContentPack.ContentSourceLabel().CapitalizeFirst()}) {UserInterfaceUtils.LabelForTileMutator(e, false)}", () => FindTileMutator(e))));

                #else

                options.AddRange(LandformManager.LandformsById.Values
                    .Where(e => !e.IsInternal && GeologicalLandformsMod.IsLandformEnabled(e))
                    .OrderBy(e => e.TranslatedNameForSelection)
                    .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () => FindLandform(e))));

                #endif

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        listing.Gap();

        if (GeologicalLandformsMod.Settings.EnableGodMode)
        {
            var landformData = Find.World.LandformData();
            bool ignoreReq = Prefs.DevMode && GeologicalLandformsMod.Settings.IgnoreWorldTileReqInGodMode;

            if (landformData != null && (tileInfo.WorldObject is not { HasMap: true } || ignoreReq))
            {
                rect = listing.GetRect(28f);

                if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.SetLandform".Translate()))
                {
                    var biomeProps = tileInfo.Biome.Properties();

                    var eligible = LandformManager.LandformsById.Values
                        .Where(e => ignoreReq || (e.GetCommonnessForTile(tileInfo, true) > 0f && biomeProps.AllowsLandform(e)))
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
                                        CoerceTileToRequirements(tileId, landformData, e);
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
        }
    }

    private static void CoerceTileToRequirements(int tileId, LandformData data, Landform landform)
    {
        if (landform.WorldTileReq != null)
        {
            var tile = Find.WorldGrid[tileId];

            if (landform.WorldTileReq.HillinessRequirement.min > 5f)
            {
                tile.hilliness = Hilliness.Impassable;
            }
            else if (tile.hilliness == Hilliness.Impassable && landform.WorldTileReq.HillinessRequirement.max <= 5f)
            {
                tile.hilliness = (Hilliness) landform.WorldTileReq.HillinessRequirement.max;
            }

            if (landform.WorldTileReq.DepthInCaveSystemRequirement.min >= 1)
            {
                data.SetCaveSystemDepthAt(tileId, (byte) (int) landform.WorldTileReq.DepthInCaveSystemRequirement.min);
            }
            else if (landform.WorldTileReq.Topology is Topology.CaveEntrance or Topology.CaveTunnel)
            {
                data.SetCaveSystemDepthAt(tileId, 1);
            }
        }
    }

    private static void FindLandform(Landform landform, bool requirePoi = false)
    {
        int tileId = Find.WorldSelector.selectedTile;
        var grid = Find.WorldGrid;

        HashSet<int> tested = [];
        HashSet<int> pending = [tileId];

        var nbData = grid.ExtNbValues();
        var nbOffsets = grid.ExtNbOffsets();

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
                        .Translate(landform?.TranslatedNameForSelection ?? "matching", dist.ToString("F2"));
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

    #if RW_1_6_OR_GREATER

    private static void FindTileMutator(TileMutatorDef tileMutator)
    {
        int tileId = Find.WorldSelector.selectedTile;
        var grid = Find.WorldGrid;

        HashSet<int> tested = [];
        HashSet<int> pending = [tileId];

        var nbData = grid.ExtNbValues();
        var nbOffsets = grid.ExtNbOffsets();

        var maxDistance = GeologicalLandformsMod.Settings.MaxLandformSearchRadius.Value;

        for (int i = 0; i < maxDistance; i++)
        {
            var copy = pending.ToList();
            pending.Clear();
            foreach (var p in copy)
            {
                if (grid[p].Mutators.Contains(tileMutator))
                {
                    CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(p));
                    Find.WorldSelector.selectedTile = p;
                    float dist = grid.ApproxDistanceInTiles(tileId, p);
                    var messageSuccess = "GeologicalLandforms.WorldMap.FindLandformSuccess"
                        .Translate(tileMutator.LabelCap, dist.ToString("F2"));
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
            .Translate(tileMutator.LabelCap, maxDistance.ToString("F0"));
        Messages.Message(messageFail, MessageTypeDefOf.RejectInput, false);
    }

    #endif
}

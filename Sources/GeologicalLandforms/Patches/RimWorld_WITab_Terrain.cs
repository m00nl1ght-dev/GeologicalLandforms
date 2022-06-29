using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (WITab_Terrain), "FillTab")]
internal static class RimWorld_WITab_Terrain
{
    private static readonly MethodInfo Method_AppendWithComma = AccessTools.Method(typeof(GenText), "AppendWithComma");
    private static readonly MethodInfo Method_LabelDouble = AccessTools.Method(typeof(Listing_Standard), "LabelDouble");
    private static readonly MethodInfo Method_GetSpecialFeatures = AccessTools.Method(typeof(RimWorld_WITab_Terrain), "GetSpecialFeatures");
    
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var foundAWC = false;
        var patched = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (foundAWC && !patched)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                    continue;
                }
                
                if (instruction.Calls(Method_LabelDouble))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method_GetSpecialFeatures);
                    patched = true;
                    continue;
                }
            }
            
            if (instruction.Calls(Method_AppendWithComma))
            {
                foundAWC = true;
            }
            
            yield return instruction;
        }
        
        if (patched == false)
            Log.Error(ModInstance.LogPrefix + "Failed to patch RimWorld_WITab_Terrain");
    }

    private static void GetSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        int tileId = Find.WorldSelector.selectedTile;
        IWorldTileInfo worldTileInfo = WorldTileInfo.Get(tileId);

        if (worldTileInfo.Landforms != null)
        {
            var mainLandform = worldTileInfo.Landforms.FirstOrDefault(l => !l.IsLayer);
            if (mainLandform != null)
            {
                var lfStr = mainLandform.TranslatedNameWithDirection(worldTileInfo.LandformDirection).CapitalizeFirst();
                listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Landform".Translate(), lfStr);
            }
        
            if (worldTileInfo.BorderingBiomes?.Count > 0)
            {
                if (worldTileInfo.Landforms.Any(l => l.OutputBiomeGrid?.BiomeTransitionKnob?.connected() ?? false))
                {
                    string bbStr = worldTileInfo.BorderingBiomes.Select(b => b.Biome.label.CapitalizeFirst()).Distinct().Join(b => b);
                    listingStandard.LabelDouble("GeologicalLandforms.WorldMap.BorderingBiomes".Translate(), bbStr);
                }
            }
        }

        StringBuilder sb = new();
        
        if (Find.World.HasCaves(tileId))
        {
            sb.AppendWithComma("HasCaves".Translate());
        }

        if (sb.Length > 0)
            listingStandard.LabelDouble("SpecialFeatures".Translate(), sb.ToString().CapitalizeFirst());

        listingStandard.Gap();
        
        var rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.FindLandform".Translate()))
            {
                var options = new List<FloatMenuOption>
                {
                    new("GeologicalLandforms.WorldMap.FindLandformAny".Translate(), () => FindLandform(null)),
                    new("GeologicalLandforms.WorldMap.FindLandformAnyPOI".Translate(), () => FindLandform(null, true))
                };
                options.AddRange(LandformManager.Landforms.Values
                    .Where(e => !e.IsLayer)
                    .OrderBy(e => e.TranslatedNameForSelection)
                    .Select(e => new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () => FindLandform(e))));
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        
        listingStandard.Gap();
        
        if (ModInstance.Settings.EnableGodMode)
        {
            var landformData = Find.World.LandformData();
            bool ignoreReq = Prefs.DevMode && ModInstance.Settings.IgnoreWorldTileReqInGodMode;
            
            if (landformData != null && (ignoreReq || (!landformData.IsLocked(tileId) && WorldTileInfo.CanHaveLandform(worldTileInfo))))
            {
                rect = listingStandard.GetRect(28f);
                if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
                {
                    if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.SetLandform".Translate()))
                    {
                        var eligible = LandformManager.Landforms.Values
                            .Where(e => ignoreReq || (e.WorldTileReq?.CheckRequirements(worldTileInfo, true) ?? false))
                            .Where(e => !e.IsLayer)
                            .ToList();
                    
                        var options = new List<FloatMenuOption>
                        {
                            new("GeologicalLandforms.WorldMap.SetLandformAuto".Translate(), () =>
                            {
                                landformData.Reset(tileId);
                            }),
                            new("None".Translate(), () =>
                            {
                                landformData.Commit(tileId, null, worldTileInfo.LandformDirection);
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
                                    Find.WindowStack.Add(new FloatMenu(dirOptions) {vanishIfMouseDistant = false});
                                }
                                else
                                {
                                    landformData.Commit(tileId, e, worldTileInfo.LandformDirection);
                                }
                            })));
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            }
            
            listingStandard.Gap();
        }
        
        if (Prefs.DevMode && ModInstance.Settings.ShowWorldTileDebugInfo)
        {
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Topology".Translate(), worldTileInfo.Topology.ToString());
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.TopologyDirection".Translate(), worldTileInfo.LandformDirection.ToStringHuman());
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Swampiness".Translate(), worldTileInfo.Swampiness.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.RiverAngle".Translate(), worldTileInfo.MainRiverAngle.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.MainRoadAngle".Translate(), worldTileInfo.MainRoadAngle.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void FindLandform(Landform landform, bool requirePoi = false)
    {
        int tileId = Find.WorldSelector.selectedTile;
        var grid = Find.WorldGrid;

        HashSet<int> tested = new();
        HashSet<int> pending = new() {tileId};
        List<int> nb = new();

        bool Matches(IWorldTileInfo tileInfo)
        {
            if (tileInfo.Landforms == null) return false;
            if (landform != null) return tileInfo.Landforms.Contains(landform);
            return tileInfo.Landforms.Any(l => !l.IsLayer && (!requirePoi || l.IsPointOfInterest()));
        }

        for (int i = 0; i < ModInstance.Settings.MaxLandformSearchRadius; i++)
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
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "GeologicalLandforms.WorldMap.FindLandformSuccess".Translate() + Math.Round(dist, 2)));
                    return;
                }

                tested.Add(p);
                
                nb.Clear();
                grid.GetTileNeighbors(p, nb);
                foreach (var nTile in nb)
                {
                    if (tested.Contains(nTile)) continue;
                    pending.Add(nTile);
                }
            }
        }
        
        Find.WindowStack.Add(new Dialog_MessageBox(
            "GeologicalLandforms.WorldMap.FindLandformFail".Translate() + ModInstance.Settings.MaxLandformSearchRadius));
    }
}
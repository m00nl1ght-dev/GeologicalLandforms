using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using HarmonyLib;
using LunarFramework.GUI;
using RimWorld;
using UnityEngine;
using Verse;

#if DEBUG
using GeologicalLandforms.GraphEditor;
#endif

namespace GeologicalLandforms;

public static class DebugActions
{
    public static void DebugActionsGUI(LayoutRect layout)
    {
        var world = Find.World;
        var map = Find.CurrentMap;

        if (map is { Tile: >= 0 })
        {
            var biomeGrid = map.BiomeGrid();

            var biomeGridActive = biomeGrid.Enabled;
            LunarGUI.Checkbox(layout, ref biomeGridActive, Label("BiomeGridEnabledForCurrentMap"));
            if (biomeGrid.Enabled != biomeGridActive) biomeGrid.Enabled = biomeGridActive;

            layout.Abs(10f);

            if (biomeGridActive)
            {
                layout.BeginAbs(Text.LineHeight, new LayoutParams { Horizontal = true, Reversed = true, Spacing = 5 });

                if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryExportToAreasShort"), Label("BiomeGridEntryExportToAreas")))
                {
                    foreach (var entry in biomeGrid.Entries)
                    {
                        var area = map.areaManager.GetLabeled(entry.ToString());
                        if (area == null)
                        {
                            area = new Area_Allowed(map.areaManager, entry.ToString());
                            map.areaManager.AllAreas.Add(area);
                        }

                        foreach (var cell in map.AllCells)
                        {
                            area[cell] = biomeGrid.EntryAt(cell) == entry;
                        }
                    }
                }

                if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryRegenerateAllShort"), Label("BiomeGridEntryRegenerateAll")))
                {
                    GenStep_BiomeVariants.RegenerateBiomeGrid(map);
                }

                if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryRemoveShort"), Label("BiomeGridEntryRemove")))
                {
                    var options = biomeGrid.Entries
                        .Where(d => d.Index != 0)
                        .Select(e => new FloatMenuOption(e.ToString(), () => RemoveBiomeGridEntry(biomeGrid, e))).ToList();
                    if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                }

                if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryAddShort"), Label("BiomeGridEntryAdd")))
                {
                    biomeGrid.MakeEntry(biomeGrid.Primary.BiomeBase, null, true);
                }

                LunarGUI.Label(layout.Abs(-1), Label("BiomeGridEntriesHeader"));

                layout.End();

                LunarGUI.SeparatorLine(layout, 3f);

                foreach (var entry in biomeGrid.Entries)
                {
                    layout.BeginAbs(Text.LineHeight, new LayoutParams { Horizontal = true });

                    LunarGUI.Label(layout.Abs(70), entry.CellCount.ToString());

                    layout.BeginAbs(-1, new LayoutParams { Horizontal = true, Reversed = true, Spacing = 5 });

                    if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntrySetFromAreaShort"), Label("BiomeGridEntrySetFromArea")))
                    {
                        var options = map.areaManager.AllAreas
                            .Where(e => e is Area_Allowed)
                            .Select(e => new FloatMenuOption(e.Label, () =>
                            {
                                foreach (var cell in e.ActiveCells)
                                {
                                    biomeGrid.SetEntry(cell, entry);
                                }
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }

                    if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryRemoveVariantShort"), Label("BiomeGridEntryRemoveVariant")))
                    {
                        var options = entry.VariantLayers
                            .Select(e => new FloatMenuOption(e.ToString(), () =>
                            {
                                entry.Set(entry.BiomeBase, entry.VariantLayers.Except(e));
                                entry.Refresh(biomeGrid.TileInfo);
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }

                    if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryAddVariantShort"), Label("BiomeGridEntryAddVariant")))
                    {
                        var options = DefDatabase<BiomeVariantDef>.AllDefs
                            .SelectMany(e => e.layers).Where(e => !entry.VariantLayers.Contains(e))
                            .Select(e => new FloatMenuOption(e.ToString(), () =>
                            {
                                entry.Set(entry.BiomeBase, entry.VariantLayers.Append(e));
                                entry.Refresh(biomeGrid.TileInfo);
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }

                    if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntrySetBaseShort"), Label("BiomeGridEntrySetBase")))
                    {
                        var options = DefDatabase<BiomeDef>.AllDefs
                            .Select(e => new FloatMenuOption(e.defName, () =>
                            {
                                entry.Set(e, entry.VariantLayers);
                                entry.Refresh(biomeGrid.TileInfo);
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }

                    if (LunarGUI.Button(layout.Abs(30), Label("BiomeGridEntryInfoShort"), Label("BiomeGridEntryInfo")))
                    {
                        LunarGUI.OpenGenericWindow(GeologicalLandformsMod.LunarAPI, new Vector2(500, 500), (_, l) => BiomeEntryDetailsGUI(l, entry));
                    }

                    LunarGUI.Label(layout.Abs(-1), entry.ToString());

                    layout.End();

                    layout.End();
                }
            }

            LunarGUI.SeparatorLine(layout, 3f);

            layout.Abs(10f);

            LunarGUI.LabelDouble(layout, Label("WholeMapDesiredPlants"), map.wildPlantSpawner.CurrentWholeMapNumDesiredPlants.ToString("F2"), false);
            LunarGUI.LabelDouble(layout, Label("OpenGroundFactor"), biomeGrid.OpenGroundFraction.ToString("F2"), false);
            LunarGUI.LabelDouble(layout, Label("AnimalDensityFactor"), GeologicalLandformsAPI.AnimalDensityFactor(biomeGrid).ToString("F2"), false);

            layout.Abs(10f);

            if (LunarGUI.Button(layout, Label("SetTerrainInArea")))
            {
                var options = map.areaManager.AllAreas
                    .Where(e => e is Area_Allowed)
                    .Select(e => new FloatMenuOption(e.Label, () =>
                    {
                        var options = DefDatabase<TerrainDef>.AllDefsListForReading
                            .Select(t => new FloatMenuOption(t.defName, () =>
                            {
                                foreach (var cell in e.ActiveCells)
                                {
                                    map.terrainGrid.SetTerrain(cell, t);
                                }
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }))
                    .ToList();
                if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
            }

            if (LunarGUI.Button(layout, Label("ReplaceStoneOnCurrentMap")))
            {
                var options = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d.IsNonResourceNaturalRock)
                    .Select(e => new FloatMenuOption(e.defName, () =>
                    {
                        var options = DefDatabase<ThingDef>.AllDefsListForReading
                            .Where(d => d.IsNonResourceNaturalRock)
                            .Select(t => new FloatMenuOption(t.defName, () =>
                            {
                                ReplaceNaturalRock(e, t);
                            }))
                            .ToList();
                        if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                    }))
                    .ToList();
                if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        if (world != null)
        {
            if (LunarGUI.Button(layout, Label("RefreshWorldTileData")))
            {
                RefreshWorldTileInfo(false);
            }

            if (LunarGUI.Button(layout, Label("RefreshWorldTileDataForced")))
            {
                RefreshWorldTileInfo(true);
            }
        }
    }

    private static void BiomeEntryDetailsGUI(LayoutRect layout, BiomeGrid.Entry entry)
    {
        var biome = entry.Biome;

        LunarGUI.Label(layout, Label("BiomeGridDetails.PotentialPlants"));
        LunarGUI.SeparatorLine(layout, 3f);

        foreach (var (plant, commonality) in biome.PlantCommonalities)
        {
            LunarGUI.LabelDouble(layout, plant.LabelCap, commonality.ToString("F2"), false);
        }

        layout.Abs(20f);

        LunarGUI.Label(layout, Label("BiomeGridDetails.PotentialAnimals"));
        LunarGUI.SeparatorLine(layout, 3f);

        foreach (var animal in biome.AllWildAnimals)
        {
            var commonality = biome.CommonalityOfAnimal(animal);
            var commonalityPollution = biome.CommonalityOfPollutionAnimal(animal);
            var str = commonalityPollution > 0f ? (commonalityPollution.ToString("F2") + " (P)") : commonality.ToString("F2");
            LunarGUI.LabelDouble(layout, animal.LabelCap, str, false);
        }
    }

    private static void RemoveBiomeGridEntry(BiomeGrid grid, BiomeGrid.Entry entry)
    {
        foreach (var cell in grid.map.AllCells)
        {
            if (grid.EntryAt(cell) == entry) grid.SetEntry(cell, grid.Primary);
        }

        var entries = grid.Entries as List<BiomeGrid.Entry>;
        entries?.Remove(entry);
    }

    public static void ReplaceNaturalRock(ThingDef filter, ThingDef replacement)
    {
        var map = Find.CurrentMap;
        map.regionAndRoomUpdater.Enabled = false;

        var terrainFilter = filter.building.naturalTerrain;
        var terrainReplacement = replacement.building.naturalTerrain;

        var graphicField = AccessTools.Field(typeof(Thing), "graphicInt");

        foreach (var allCell in map.AllCells)
        {
            var building = map.edificeGrid[allCell];
            if (building != null)
            {
                if (building.def == filter)
                {
                    building.def = replacement;
                    graphicField.SetValue(building, null);
                    building.DirtyMapMesh(map);
                }
                else if (building.def == filter.building.smoothedThing)
                {
                    building.def = replacement.building.smoothedThing;
                    graphicField.SetValue(building, null);
                    building.DirtyMapMesh(map);
                }
            }

            var terrain = map.terrainGrid.TerrainAt(allCell);
            if (terrain == terrainFilter)
            {
                map.terrainGrid.SetTerrain(allCell, terrainReplacement);
            }
            else if (terrain == terrainFilter.smoothedTerrain)
            {
                map.terrainGrid.SetTerrain(allCell, terrainReplacement.smoothedTerrain);
            }
            else if (terrain == filter.building.leaveTerrain)
            {
                map.terrainGrid.SetTerrain(allCell, replacement.building.leaveTerrain);
            }
        }

        foreach (var thing in map.listerThings.AllThings)
        {
            if (thing.def == filter.building.mineableThing)
            {
                thing.def = replacement.building.mineableThing;
                graphicField.SetValue(thing, null);
                thing.DirtyMapMesh(map);
            }
        }

        map.regionAndRoomUpdater.Enabled = true;
    }

    public static void RefreshWorldTileInfo(bool force)
    {
        if (force) Find.World.LandformData().ResetAll();

        WorldTileInfo.CreateNewCache();
        FillWorldTileInfoCache();

        Find.World.renderer.SetDirty<WorldLayer_Landforms>();
    }

    public static void FillWorldTileInfoCache()
    {
        WorldTileInfo.CreateNewCache();

        GC.Collect();
        var before = GC.GetTotalMemory(true) / 1000000f;

        var world = Find.World;
        for (int i = 0; i < world.grid.TilesCount; i++) WorldTileInfo.Get(i);

        GC.Collect();
        var after = GC.GetTotalMemory(true) / 1000000f;

        GeologicalLandformsMod.Logger.Log($"Filled cache for {world.grid.TilesCount} tiles, cache is now using {after - before:F2} MB of memory.");
    }

    private static string Label(string translationKey) => ("GeologicalLandforms.Settings.Debug." + translationKey).Translate();

    #if DEBUG

    public static void SetupDebugActions()
    {
        GeologicalLandformsMod.LunarAPI.LifecycleHooks.DoOnGUI(() =>
        {
            if (Prefs.DevMode && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.L) && !LandformGraphEditor.IsEditorOpen)
            {
                Find.WindowStack.Add(new LandformGraphEditor());

                if (WorldTileUtils.CurrentWorldTile is WorldTileInfo tileInfo)
                {
                    var landform = tileInfo.Landforms?.FirstOrDefault(lf => lf.IsLayer) ?? tileInfo.Landforms?.FirstOrDefault();
                    if (landform != null) LandformGraphEditor.ActiveEditor?.OpenLandform(landform);
                }
            }
        });
    }

    #endif
}

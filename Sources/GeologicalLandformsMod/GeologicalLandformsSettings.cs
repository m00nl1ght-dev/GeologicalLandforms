using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.GUI;
using LunarFramework.Utility;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class GeologicalLandformsSettings : LunarModSettings
{
    public readonly Entry<int> MaxLandformSearchRadius = MakeEntry(100);
    public readonly Entry<float> AnimalDensityFactorForSecludedAreas = MakeEntry(0.5f);
    
    public readonly Entry<bool> EnableCellFinderOptimization = MakeEntry(true);
    public readonly Entry<bool> EnableLandformScaling = MakeEntry(true);
    public readonly Entry<bool> EnableExperimentalLandforms = MakeEntry(false);
    public readonly Entry<bool> EnableGodMode = MakeEntry(false);
    public readonly Entry<bool> IgnoreWorldTileReqInGodMode = MakeEntry(false);
    public readonly Entry<bool> ShowWorldTileDebugInfo = MakeEntry(false);

    public readonly Entry<List<string>> DisabledLandforms = MakeEntry(new List<string>());
    public readonly Entry<List<string>> DisabledBiomeVariants = MakeEntry(new List<string>());
    
    protected override string TranslationKeyPrefix => "GeologicalLandforms.Settings";

    public GeologicalLandformsSettings() : base(GeologicalLandformsMod.LunarAPI)
    {
        MakeTab("Tab.General", DoGeneralSettingsTab);
        
        if (LandformManager.Landforms.Count > 0) 
            MakeTab("Tab.Landforms", DoLandformsSettingsTab);
        
        if (DefDatabase<BiomeVariantDef>.DefCount > 0) 
            MakeTab("Tab.BiomeVariants", DoBiomeVariantsSettingsTab);
        
        if (Prefs.DevMode) 
            MakeTab("Tab.Debug", DoDebugSettingsTab);
    }
    
    private void DoGeneralSettingsTab(LayoutRect layout)
    {
        if (LunarGUI.Button(layout, Label("OpenEditor")))
        {
            Find.WindowStack.Add(new LandformGraphEditor());
        }

        if (LunarGUI.Button(layout, Label("OpenDataDir")))
        {
            Application.OpenURL(LandformManager.CustomLandformsDir(LandformManager.CurrentVersion));
        }

        if (LunarGUI.Button(layout, Label("ResetAll")))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(Label("ConfirmResetAll"), ResetAll));
        }

        layout.Abs(10f);
        
        LunarGUI.LabelDouble(layout, Label("MaxLandformSearchRadius"), MaxLandformSearchRadius.Value.ToString("F0"));
        LunarGUI.Slider(layout, ref MaxLandformSearchRadius.Value, 10, 500);
        
        LunarGUI.LabelDouble(layout, Label("AnimalDensityFactorForSecludedAreas"), AnimalDensityFactorForSecludedAreas.Value.ToString("F2"));
        LunarGUI.Slider(layout, ref AnimalDensityFactorForSecludedAreas.Value, 0.25f, 0.75f);
        
        layout.Abs(10f);

        LunarGUI.Checkbox(layout, ref EnableExperimentalLandforms.Value, Label("EnableExperimentalLandforms"));
        LunarGUI.Checkbox(layout, ref EnableGodMode.Value, Label("EnableGodMode"));
        LunarGUI.Checkbox(layout, ref EnableCellFinderOptimization.Value, Label("EnableCellFinderOptimization"));
        LunarGUI.Checkbox(layout, ref EnableLandformScaling.Value, Label("EnableLandformScaling"));
    }
    
    private void DoLandformsSettingsTab(LayoutRect layout)
    {
        foreach (var landform in LandformManager.Landforms.Values)
        {
            if (landform.Manifest.IsExperimental && !EnableExperimentalLandforms) continue;
            if (landform.WorldTileReq == null) continue;
            
            LunarGUI.PushChanged();
            
            var enabled = !DisabledLandforms.Value.Contains(landform.Id);
            LunarGUI.Checkbox(layout, ref enabled, LabelForLandform(landform));
            
            if (LunarGUI.PopChanged())
            {
                if (enabled) DisabledLandforms.Value.Remove(landform.Id);
                else DisabledLandforms.Value.AddDistinct(landform.Id);
                ApplyLandformConfigEffects();
            }
        }
    }

    private void DoBiomeVariantsSettingsTab(LayoutRect layout)
    {
        foreach (var biomeVariant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
        {
            LunarGUI.PushChanged();
            
            var enabled = !DisabledBiomeVariants.Value.Contains(biomeVariant.defName);
            LunarGUI.Checkbox(layout, ref enabled, LabelForBiomeVariant(biomeVariant));
            
            if (LunarGUI.PopChanged())
            {
                if (enabled) DisabledBiomeVariants.Value.Remove(biomeVariant.defName);
                else DisabledBiomeVariants.Value.AddDistinct(biomeVariant.defName);
            }
        }
    }

    private static readonly List<string> LabelBuffer = new();
    
    private string LabelForLandform(Landform landform)
    {
        var label = landform.TranslatedNameForSelection.CapitalizeFirst();
        
        if (landform.Manifest.IsCustom) 
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Custom".Translate());
        if (landform.Manifest.IsExperimental) 
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.Experimental".Translate());
        if (landform.ModContentPack != null && landform.ModContentPack != GeologicalLandformsMod.Settings.Mod.Content) 
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(landform.ModContentPack.Name));
        if (landform.WorldTileReq.Commonness <= 0)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ZeroCommonness".Translate());
        if (landform.WorldTileReq is { Topology: Topology.CliffOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCliff".Translate());
        if (landform.WorldTileReq is { Topology: Topology.CoastOneSide, Commonness: >= 1f } && !landform.IsLayer)
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.ReplacesVanillaCoast".Translate());

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }
    
    private string LabelForBiomeVariant(BiomeVariantDef biomeVariant)
    {
        var label = biomeVariant.label.CapitalizeFirst();
        
        if (biomeVariant.modContentPack != null && biomeVariant.modContentPack != GeologicalLandformsMod.Settings.Mod.Content) 
            LabelBuffer.Add("GeologicalLandforms.Settings.Landforms.DefinedInMod".Translate(biomeVariant.modContentPack.Name));

        if (LabelBuffer.Count > 0) label += " <color=#777777>(" + LabelBuffer.Join(s => s) + ")</color>";
        LabelBuffer.Clear();
        return label;
    }

    public void ApplyLandformConfigEffects()
    {
        GeologicalLandformsAPI.DisableVanillaMountainGeneration = LandformManager.Landforms.Values
            .Where(GeologicalLandformsMod.IsLandformEnabled)
            .Any(lf => !lf.IsLayer && lf.WorldTileReq is { Topology: Topology.CliffOneSide, Commonness: >= 1f });
    }
    
    private void DoDebugSettingsTab(LayoutRect layout)
    {
        LunarGUI.Checkbox(layout, ref ShowWorldTileDebugInfo.Value, Label("ShowWorldTileDebugInfo"));
            
        if (EnableGodMode)
        {
            LunarGUI.Checkbox(layout, ref IgnoreWorldTileReqInGodMode.Value, Label("IgnoreWorldTileReqInGodMode"));
        }
        
        layout.Abs(10f);
        
        if (Find.CurrentMap != null && LunarGUI.Button(layout, "[DEV] Replace all stone on current map"))
        {
            var options = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsNonResourceNaturalRock)
                .Select(e => new FloatMenuOption(e.defName, () => ReplaceNaturalRock(e))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private void ReplaceNaturalRock(ThingDef thingDef)
    {
        var map = Find.CurrentMap;
        map.regionAndRoomUpdater.Enabled = false;

        var terrainDef = thingDef.building.naturalTerrain;
            
        foreach (var allCell in map.AllCells)
        {
            if (map.edificeGrid[allCell]?.def?.IsNonResourceNaturalRock ?? false)
                GenSpawn.Spawn(thingDef, allCell, map);
                
            if (map.terrainGrid.TerrainAt(allCell)?.smoothedTerrain != null)
            {
                map.terrainGrid.SetTerrain(allCell, terrainDef);
            }
        }
            
        map.regionAndRoomUpdater.Enabled = true;
    }

    public override void ResetAll()
    {
        LandformManager.ResetAll();
        base.ResetAll();
    }
}
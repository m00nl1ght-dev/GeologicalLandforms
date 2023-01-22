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

public class GeologicalLandformsSettings : ModSettings
{
    public int MaxLandformSearchRadius = 100;
    public float AnimalDensityFactorForSecludedAreas = 0.5f;

    public bool EnableCellFinderOptimization = true;
    public bool EnableLandformScaling = true;
    public bool EnableExperimentalLandforms;
    
    public bool EnableGodMode;
    public bool IgnoreWorldTileReqInGodMode;
    public bool ShowWorldTileDebugInfo;

    public List<string> DisabledLandforms = new();
    public List<string> DisabledBiomeVariants = new();

    private readonly LayoutRect _layout = new(GeologicalLandformsMod.LunarAPI);

    private Tab _tab = Tab.General;
    private List<TabRecord> _tabs;
    private Vector2 _scrollPos;
    private Rect _viewRect;

    public void DoSettingsWindowContents(Rect rect)
    {
        if (!GeologicalLandformsMod.LunarAPI.IsInitialized())
        {
            _layout.BeginRoot(rect);
            LunarGUI.Label(_layout, "An error occured whie loading this mod. Check the log file for more information.");
            _layout.End();
            return;
        }

        if (_tabs == null)
        {
            _tabs = new() { new("GeologicalLandforms.Settings.Tab.General".Translate(), () => _tab = Tab.General, () => _tab == Tab.General) };

            if (LandformManager.Landforms.Count > 0)
                _tabs.Add(new("GeologicalLandforms.Settings.Tab.Landforms".Translate(), () => _tab = Tab.Landforms, () => _tab == Tab.Landforms));

            if (DefDatabase<BiomeVariantDef>.DefCount > 0)
                _tabs.Add(new("GeologicalLandforms.Settings.Tab.BiomeVariants".Translate(), () => _tab = Tab.BiomeVariants, () => _tab == Tab.BiomeVariants));
            
            if (Prefs.DevMode) 
                _tabs.Add(new("GeologicalLandforms.Settings.Tab.Debug".Translate(), () => _tab = Tab.Debug, () => _tab == Tab.Debug));
        }

        rect.yMin += 35;
        rect.yMax -= 12;

        Widgets.DrawMenuSection(rect);
        TabDrawer.DrawTabs(rect, _tabs);
        
        rect = rect.ContractedBy(18f);
        
        switch (_tab)
        {
            case Tab.General: DoGeneralSettingsTab(rect); break;
            case Tab.Landforms: DoLandformsSettingsTab(rect); break;
            case Tab.BiomeVariants: DoBiomeVariantsSettingsTab(rect); break;
            case Tab.Debug: DoDebugSettingsTab(rect); break;
        }
    }
    
    private void DoGeneralSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });

        if (LunarGUI.Button(_layout, "GeologicalLandforms.Settings.OpenEditor".Translate()))
        {
            Find.WindowStack.Add(new LandformGraphEditor());
        }

        if (LunarGUI.Button(_layout, "GeologicalLandforms.Settings.OpenDataDir".Translate()))
        {
            Application.OpenURL(LandformManager.CustomLandformsDir(LandformManager.CurrentVersion));
        }

        if (LunarGUI.Button(_layout, "GeologicalLandforms.Settings.ResetAll".Translate()))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeologicalLandforms.Settings.ConfirmResetAll".Translate(), ResetAll));
        }

        _layout.Abs(10f);
        
        LunarGUI.DoubleLabel(_layout, "GeologicalLandforms.Settings.MaxLandformSearchRadius".Translate(), MaxLandformSearchRadius.ToString("F0"));
        LunarGUI.Slider(_layout, ref MaxLandformSearchRadius, 10, 500);
        
        LunarGUI.DoubleLabel(_layout, "GeologicalLandforms.Settings.AnimalDensityFactorForSecludedAreas".Translate(), AnimalDensityFactorForSecludedAreas.ToString("F2"));
        LunarGUI.Slider(_layout, ref AnimalDensityFactorForSecludedAreas, 0.25f, 0.75f);
        
        _layout.Abs(10f);
        
        LunarGUI.Checkbox(_layout, ref EnableExperimentalLandforms, "GeologicalLandforms.Settings.EnableExperimentalLandforms".Translate());
        LunarGUI.Checkbox(_layout, ref EnableGodMode, "GeologicalLandforms.Settings.EnableGodMode".Translate());
        LunarGUI.Checkbox(_layout, ref EnableCellFinderOptimization, "GeologicalLandforms.Settings.EnableCellFinderOptimization".Translate());
        LunarGUI.Checkbox(_layout, ref EnableLandformScaling, "GeologicalLandforms.Settings.EnableLandformScaling".Translate());

        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
    }
    
    private void DoLandformsSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });

        DisabledLandforms ??= new();

        foreach (var landform in LandformManager.Landforms.Values)
        {
            if (landform.Manifest.IsExperimental && !EnableExperimentalLandforms) continue;
            if (landform.WorldTileReq == null) continue;
            
            LunarGUI.PushChanged();
            
            var enabled = !DisabledLandforms.Contains(landform.Id);
            LunarGUI.Checkbox(_layout, ref enabled, LabelForLandform(landform));
            
            if (LunarGUI.PopChanged())
            {
                if (enabled) DisabledLandforms.Remove(landform.Id);
                else DisabledLandforms.AddDistinct(landform.Id);
                ApplyLandformConfigEffects();
            }
        }

        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
    }

    private void DoBiomeVariantsSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });
        
        DisabledBiomeVariants ??= new();

        foreach (var biomeVariant in DefDatabase<BiomeVariantDef>.AllDefsListForReading)
        {
            LunarGUI.PushChanged();
            
            var enabled = !DisabledBiomeVariants.Contains(biomeVariant.defName);
            LunarGUI.Checkbox(_layout, ref enabled, LabelForBiomeVariant(biomeVariant));
            
            if (LunarGUI.PopChanged())
            {
                if (enabled) DisabledBiomeVariants.Remove(biomeVariant.defName);
                else DisabledBiomeVariants.AddDistinct(biomeVariant.defName);
            }
        }

        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
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
    
    private void DoDebugSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });

        LunarGUI.Checkbox(_layout, ref ShowWorldTileDebugInfo, "GeologicalLandforms.Settings.ShowWorldTileDebugInfo".Translate());
            
        if (EnableGodMode)
        {
            LunarGUI.Checkbox(_layout, ref IgnoreWorldTileReqInGodMode, "GeologicalLandforms.Settings.IgnoreWorldTileReqInGodMode".Translate());
        }
        
        _layout.Abs(10f);
        
        if (Find.CurrentMap != null && LunarGUI.Button(_layout, "[DEV] Replace all stone on current map"))
        {
            var options = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsNonResourceNaturalRock)
                .Select(e => new FloatMenuOption(e.defName, () => ReplaceNaturalRock(e))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
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

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxLandformSearchRadius, "MaxLandformSearchRadius", 100);
        Scribe_Values.Look(ref AnimalDensityFactorForSecludedAreas, "AnimalDensityFactorForSecludedAreas", 0.5f);
        Scribe_Values.Look(ref EnableCellFinderOptimization, "EnableCellFinderOptimization", true);
        Scribe_Values.Look(ref EnableLandformScaling, "EnableLandformScaling", true);
        Scribe_Values.Look(ref EnableExperimentalLandforms, "EnableExperimentalLandforms");
        Scribe_Values.Look(ref ShowWorldTileDebugInfo, "ShowWorldTileDebugInfo");
        Scribe_Values.Look(ref EnableGodMode, "EnableGodMode");
        Scribe_Values.Look(ref IgnoreWorldTileReqInGodMode, "IgnoreWorldTileReqInGodMode");
        Scribe_Collections.Look(ref DisabledLandforms, "DisabledLandforms", LookMode.Value);
        Scribe_Collections.Look(ref DisabledBiomeVariants, "DisabledBiomeVariants", LookMode.Value);
        
        base.ExposeData();
    }
    
    public void ResetAll()
    {
        LandformManager.ResetAll();
        ShowWorldTileDebugInfo = false;
        EnableCellFinderOptimization = true;
        EnableLandformScaling = true;
        EnableExperimentalLandforms = false;
        EnableGodMode = false;
        IgnoreWorldTileReqInGodMode = false;
        MaxLandformSearchRadius = 100;
        AnimalDensityFactorForSecludedAreas = 0.5f;
        DisabledLandforms = new();
        DisabledBiomeVariants = new();
    }
    
    public enum Tab
    {
        General, Landforms, BiomeVariants, Debug
    }
}
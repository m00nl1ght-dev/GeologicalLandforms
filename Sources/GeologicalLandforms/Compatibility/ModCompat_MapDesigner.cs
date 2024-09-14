using System.Reflection;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using MapPreview;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
internal class ModCompat_MapDesigner : ModCompat
{
    public override string TargetAssemblyName => "MapDesigner";
    public override string DisplayName => "Map Designer";

    private object _settings;
    private FieldInfo _field_coastDir;
    private FieldInfo _field_beachTerr;

    private static bool _overrideGL;

    protected override bool OnApply()
    {
        var modType = FindType("MapDesigner.MapDesignerMod");
        var mod = Require(LoadedModManager.GetMod(modType));

        _settings = Require(Require(AccessTools.Field(modType, "settings")).GetValue(mod));
        _field_coastDir = Require(AccessTools.Field(_settings.GetType(), "coastDir"));
        _field_beachTerr = Require(AccessTools.Field(_settings.GetType(), "beachTerr"));

        GeologicalLandformsAPI.LandformEnabled.AddModifier(99, (lf, val) =>
            val && (!_overrideGL || !lf.IsLayer || lf.LayerConfig.LayerId != "river"));

        GeologicalLandformsAPI.WorldTileInfoHook.AddObserver(1, info =>
        {
            if (_overrideGL && info.Topology.IsCoast())
            {
                byte coastDir = (byte) _field_coastDir.GetValue(_settings);
                if (coastDir is not (0 or > 4))
                {
                    info.TopologyDirection = new Rot4(coastDir - 1);
                }
            }
        });

        NodeTerrainNaturalWater.OnCalculate += data =>
        {
            string beachTerr = (string) _field_beachTerr.GetValue(_settings);
            if (_overrideGL && beachTerr != null && beachTerr != "Vanilla")
            {
                var terrainDef = DefDatabase<TerrainDef>.GetNamedSilentFail(beachTerr);
                if (terrainDef != null)
                {
                    data.Beach = terrainDef;
                }
            }
        };

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("MapDesigner.UI.RiversCard", "DrawRiversCard")]
    private static bool RiversCard_DrawRiversCard_Prefix(ref Rect rect)
    {
        var cbRect = rect.TopPartPixels(40f);
        cbRect.xMin += 5f;
        cbRect.xMax -= 20f;

        var pre = _overrideGL;

        Widgets.CheckboxLabeled(cbRect, "GeologicalLandforms.Integration.MapDesigner.RiverCardOverride".Translate(), ref _overrideGL);

        if (pre != _overrideGL) MapPreviewAPI.NotifyWorldChanged();

        rect.yMin += 40f;
        return _overrideGL;
    }

    [HarmonyPostfix]
    [HarmonyPatch("MapDesigner.UI.GeneralCard", "ResetAllSettings")]
    private static void GeneralCard_ResetAllSettings_Postfix()
    {
        _overrideGL = false;
        MapPreviewAPI.NotifyWorldChanged();
    }
}

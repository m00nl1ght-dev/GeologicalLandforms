using System.Reflection;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
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

    protected override bool OnApply()
    {
        var modType = FindType("MapDesigner.MapDesignerMod");
        var mod = Require(LoadedModManager.GetMod(modType));

        _settings = Require(Require(AccessTools.Field(modType, "settings")).GetValue(mod));
        _field_coastDir = Require(AccessTools.Field(_settings.GetType(), "coastDir"));
        _field_beachTerr = Require(AccessTools.Field(_settings.GetType(), "beachTerr"));

        GeologicalLandformsAPI.WorldTileInfoHook += info =>
        {
            if (info.Topology.IsCoast())
            {
                byte coastDir = (byte) _field_coastDir.GetValue(_settings);
                if (coastDir is not (0 or > 4))
                {
                    info.TopologyDirection = new Rot4(coastDir - 1);
                }
            }
        };

        NodeTerrainNaturalWater.OnCalculate += data =>
        {
            string beachTerr = (string) _field_beachTerr.GetValue(_settings);
            if (beachTerr != null && beachTerr != "Vanilla")
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
}

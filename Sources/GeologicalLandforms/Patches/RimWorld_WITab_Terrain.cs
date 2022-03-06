using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
            Log.Error("Failed to patch RimWorld_WITab_Terrain");
    }

    private static void GetSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        StringBuilder sb = new();
        
        int tileId = Find.WorldSelector.selectedTile;
        WorldTileInfo worldTileInfo = WorldTileInfo.GetWorldTileInfo(tileId);

        Landform landform = Main.Settings.Landforms.TryGetValue(worldTileInfo.LandformId);
        
        if (landform != null)
        {
            string append = landform.TranslatedName;
            
            if (landform.DisplayNameHasDirection)
            {
                if (worldTileInfo.Topology is Topology.CoastTwoSides or Topology.CliffTwoSides)
                {
                    append = TranslateDoubleRot4(worldTileInfo.LandformDirection) + " " + append;
                }
                else
                {
                    append = TranslateRot4(worldTileInfo.LandformDirection) + " " + append;
                }
            }
            
            sb.AppendWithComma(append);
        }
        
        if (Find.World.HasCaves(tileId))
        {
            sb.AppendWithComma("HasCaves".Translate());
        }
        
        if (Prefs.DevMode)
        {
            listingStandard.LabelDouble("Topology", worldTileInfo.Topology.ToString());
            listingStandard.LabelDouble("TopologyDirection", worldTileInfo.LandformDirection.ToStringHuman());
            listingStandard.LabelDouble("Swampiness", worldTileInfo.Tile.swampiness.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("RiverWidth", worldTileInfo.RiverWidth.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("RiverAngle", worldTileInfo.RiverAngle.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("MainRoadMultiplier", worldTileInfo.MainRoadMultiplier.ToString(CultureInfo.InvariantCulture));
            listingStandard.LabelDouble("MainRoadAngle", worldTileInfo.MainRoadAngle.ToString(CultureInfo.InvariantCulture));
        }

        if (sb.Length > 0)
            listingStandard.LabelDouble("SpecialFeatures".Translate(), sb.ToString().CapitalizeFirst());
    }

    private static string TranslateRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4." + rot4.AsInt).Translate();
    }
    
    private static string TranslateDoubleRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4.Double." + rot4.AsInt).Translate();
    }
}
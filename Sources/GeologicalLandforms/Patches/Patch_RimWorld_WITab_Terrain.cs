using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WITab_Terrain))]
internal static class Patch_RimWorld_WITab_Terrain
{
    private static readonly MethodInfo Method_AppendWithComma = AccessTools.Method(typeof(GenText), "AppendWithComma");
    private static readonly MethodInfo Method_LabelDouble = AccessTools.Method(typeof(Listing_Standard), "LabelDouble");
    private static readonly MethodInfo Method_GetSpecialFeatures = AccessTools.Method(typeof(Patch_RimWorld_WITab_Terrain), "GetSpecialFeatures");
    
    [HarmonyTranspiler]
    [HarmonyPatch("FillTab")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillTab(IEnumerable<CodeInstruction> instructions)
    {
        var foundAWC = false;
        var patched = false;

        foreach (var instruction in instructions)
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
            GeologicalLandformsAPI.Logger.Fatal("Failed to patch RimWorld_WITab_Terrain");
    }

    private static void GetSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        int tileId = Find.WorldSelector.selectedTile;
        IWorldTileInfo worldTileInfo = WorldTileInfo.Get(tileId);

        if (worldTileInfo.Landforms != null)
        {
            var mainLandform = worldTileInfo.Landforms.LastOrDefault(l => !l.IsLayer);
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
        
        GeologicalLandformsAPI.RunOnTerrainTab(listingStandard);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeologicalLandforms.GraphEditor;
using GeologicalLandforms.Patches;
using HarmonyLib;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.ModCompat;

[StaticConstructorOnStartup]
internal static class ModCompat_DubsMintMenus
{
    static ModCompat_DubsMintMenus()
    {
        try
        {
            var psType = GenTypes.GetTypeInAnyAssembly("DubsMintMenus.Dialog_FancyDanPlantSetterBob");
            if (psType != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for Dubs Mint Menus.");
                Harmony harmony = new("Geological Landforms DubsMintMenus Compat");
                
                var methodIpa = AccessTools.Method(psType, "IsPlantAvailable");

                var mainPatch = typeof(RimWorld_WildPlantSpawner);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod methodIpaPrefix = new(mainPatch.GetMethod("IsPlantAvailable", bindingFlags));
                harmony.Patch(methodIpa, methodIpaPrefix);
            }
        }
        catch
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for Dubs Mint Menus!");
        }
    }
}
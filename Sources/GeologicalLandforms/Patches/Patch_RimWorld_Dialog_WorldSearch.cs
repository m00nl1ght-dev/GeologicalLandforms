#if RW_1_6_OR_GREATER

using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Dialog_WorldSearch))]
internal static class Patch_RimWorld_Dialog_WorldSearch
{
    [HarmonyPostfix]
    [HarmonyPatch("InitializeSearchSet")]
    private static void InitializeSearchSet_Postfix(List<WorldSearchElement> ___searchSet)
    {
        foreach (var element in ___searchSet)
        {
            if (element.tile.Layer.IsRootSurface && element.tile.tileId >= 0)
            {
                element.mutators = TileMutatorsCustomization.Get(element.tile.tileId, element.mutators) as List<TileMutatorDef>;

                if (element.landmark != null)
                {
                    foreach (var mutatorChance in element.landmark.def.mutatorChances)
                    {
                        // Ignore landmarks if their required tile mutator is disabled by the user via mod settings
                        if (mutatorChance.required && (element.mutators == null || !element.mutators.Contains(mutatorChance.mutator)))
                        {
                            element.landmark = null;
                            break;
                        }
                    }
                }
            }
        }
    }
}

#endif

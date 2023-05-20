using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Compatibility;

/// <summary>
/// Delays the WorldReachability patch in RimWar until the world has finished generating.
/// </summary>
[HarmonyPatch]
public class ModCompat_RimWar : ModCompat
{
    public override string TargetAssemblyName => "RimWar";
    public override string DisplayName => "RimWar";

    [HarmonyPrefix]
    [HarmonyPatch("RimWar.WorldReachability_CanReach_Patch", "Prefix")]
    private static bool WorldReachability_CanReach_Patch(ref bool __result)
    {
        if (!Find.World.HasFinishedGenerating())
        {
            __result = true;
            return false;
        }

        return true;
    }
}

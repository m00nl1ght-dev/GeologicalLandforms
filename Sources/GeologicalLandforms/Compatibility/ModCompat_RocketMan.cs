using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace GeologicalLandforms.Compatibility;

/// <summary>
/// Delays the WorldReachability patch in RocketMan until the world has finished generating.
/// </summary>
[HarmonyPatch]
public class ModCompat_RocketMan : ModCompat
{
    public override string TargetAssemblyName => "Cosmodrome";
    public override string DisplayName => "RocketMan";

    private static MethodInfo _targetMethod;

    protected override bool OnApply()
    {
        var baseType = AccessTools.TypeByName("RocketMan.Optimizations.WorldReachability_Patch");
        var nestedType = baseType?.GetNestedType("WorldReachability_CanReach_Patch");
        _targetMethod = AccessTools.Method(nestedType, "Prefix");
        return _targetMethod != null;
    }

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() => _targetMethod;

    [HarmonyPrefix]
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

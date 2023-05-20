using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Patching;
using LunarFramework.Utility;

namespace GeologicalLandforms.Compatibility;

[HotSwappable]
[HarmonyPatch]
public class ModCompat_RealisticPlanets : ModCompat
{
    public override string TargetAssemblyName => "Realistic_Planets_Continued";
    public override string DisplayName => "Realistic Planets Continued";

    [HarmonyPostfix]
    [HarmonyPatch("Planets_Code.Planets_CreateWorldParams", "DoWindowContents")]
    private static void Planets_CreateWorldParams_DoWindowContents()
    {
        Patch_RimWorld_Page_CreateWorldParams.DoExtraSliders(520f, 200f);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Planets_Code.Planets_CreateWorldParams", "Reset")]
    internal static void Reset()
    {
        Patch_RimWorld_Page_CreateWorldParams.Reset();
    }
}

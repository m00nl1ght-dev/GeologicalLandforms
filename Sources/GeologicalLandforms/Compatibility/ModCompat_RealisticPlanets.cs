using GeologicalLandforms.Patches;
using HarmonyLib;
using LunarFramework.Patching;

namespace GeologicalLandforms.Compatibility;

[HarmonyPatch]
public class ModCompat_RealisticPlanets : ModCompat
{
    public override string TargetAssemblyName => "Realistic_Planets_Continued";
    public override string DisplayName => "Realistic Planets Continued";

    private static float _originPos;

    protected override bool OnApply()
    {
        _originPos = 520f;
        
        if (AccessTools.TypeByName("WorldGenRules.MyLittlePlanet") != null)
        {
            _originPos += 40f;
        }
        
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Planets_Code.Planets_CreateWorldParams", "DoWindowContents")]
    private static void Planets_CreateWorldParams_DoWindowContents()
    {
        Patch_RimWorld_Page_CreateWorldParams.DoExtraSliders(_originPos, 200f);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Planets_Code.Planets_CreateWorldParams", "Reset")]
    internal static void Reset()
    {
        Patch_RimWorld_Page_CreateWorldParams.Reset();
    }
}

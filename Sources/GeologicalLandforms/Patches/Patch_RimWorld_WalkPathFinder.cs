using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WalkPathFinder))]
internal static class Patch_RimWorld_WalkPathFinder
{
    private static readonly Type Self = typeof(Patch_RimWorld_WalkPathFinder);

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        #if RW_1_6_OR_GREATER
        return AccessTools.FindIncludingInnerTypes(typeof(WalkPathFinder), type => AccessTools.FirstMethod(type, method =>
            method.Name.Contains("<TryFindWalkPath>") && method.Name.Contains("ValidCell") && method.ReturnType == typeof(bool)
        ));
        #else
        return AccessTools.Method(typeof(WalkPathFinder), "TryFindWalkPath");
        #endif
    }

    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> TryFindWalkPath_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("TryFindWalkPath")
            .MatchCall(typeof(GridsUtility), "Roofed", [typeof(IntVec3), typeof(Map)]).Remove()
            .Insert(CodeInstruction.Call(Self, nameof(RoofedAndTileNotImpassable)));

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static bool RoofedAndTileNotImpassable(IntVec3 pos, Map map)
    {
        return pos.Roofed(map) && map.TileInfo.hilliness != Hilliness.Impassable;
    }
}

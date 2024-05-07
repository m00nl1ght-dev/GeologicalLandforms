using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Page_CreateWorldParams))]
internal static class Patch_RimWorld_Page_CreateWorldParams
{
    internal static readonly Type Self = typeof(Patch_RimWorld_Page_CreateWorldParams);

    private static float _mountains;
    private static float _caveSystems;

    [HarmonyPostfix]
    [HarmonyPatch("Reset")]
    internal static void Reset()
    {
        _mountains = 1f;
        _caveSystems = 1f;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("DoWindowContents")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> DoWindowContents_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var ldlocPos = new CodeInstruction(OpCodes.Ldloc);
        var stlocPos = new CodeInstruction(OpCodes.Stloc);
        var ldlocWidth = new CodeInstruction(OpCodes.Ldloc);

        var patternPos = TranspilerPattern.Build("DoWindowContentsPos")
            .Match(OpCodes.Ldc_R4).Keep()
            .MatchLdloc().StoreOperandIn(ldlocPos, stlocPos).Keep()
            .MatchLdloc().StoreOperandIn(ldlocWidth).Keep()
            .Match(OpCodes.Ldc_R4).Keep()
            .Match(ci => (ConstructorInfo) ci.operand == AccessTools.Constructor(typeof(Rect), new[] { typeof(float), typeof(float), typeof(float), typeof(float) })).Keep()
            .Greedy();

        var pattern = TranspilerPattern.Build("DoWindowContents")
            .OnlyMatchAfter(patternPos)
            .MatchStore(typeof(Page_CreateWorldParams), "population").Keep()
            .Insert(ldlocPos).Insert(ldlocWidth)
            .Insert(CodeInstruction.Call(Self, nameof(DoExtraSliders)))
            .Insert(stlocPos);

        return TranspilerPattern.Apply(instructions, patternPos, pattern);
    }

    internal static float DoExtraSliders(float pos, float width)
    {
        pos += 40f;

        Widgets.Label(new Rect(0.0f, pos, 200f, 30f), "GeologicalLandforms.WorldParams.Mountains".Translate());

        #if RW_1_4
            _mountains = Widgets.HorizontalSlider_NewTemp(new Rect(200f, pos, width, 30f), _mountains, 0f, 2f, true, _mountains.ToStringPercent(), roundTo: 0.05f);
        #else
            _mountains = Widgets.HorizontalSlider(new Rect(200f, pos, width, 30f), _mountains, 0f, 2f, true, _mountains.ToStringPercent(), roundTo: 0.05f);
        #endif

        pos += 40f;

        Widgets.Label(new Rect(0.0f, pos, 200f, 30f), "GeologicalLandforms.WorldParams.CaveSystems".Translate());

        #if RW_1_4
            _caveSystems = Widgets.HorizontalSlider_NewTemp(new Rect(200f, pos, width, 30f), _caveSystems, 0f, 2f, true, _caveSystems.ToStringPercent(), roundTo: 0.05f);
        #else
            _caveSystems = Widgets.HorizontalSlider(new Rect(200f, pos, width, 30f), _caveSystems, 0f, 2f, true, _caveSystems.ToStringPercent(), roundTo: 0.05f);
        #endif

        Patch_RimWorld_WorldGenStep_Terrain.HillinessNoiseOffset = _mountains <= 0f ? 1f : -0.2f * (_mountains - 1f);
        Patch_RimWorld_WorldGenStep_Terrain.CaveSystemNoiseThreshold = _caveSystems <= 0f ? 1f : 0.4f - _caveSystems * 0.7f;

        return pos;
    }
}

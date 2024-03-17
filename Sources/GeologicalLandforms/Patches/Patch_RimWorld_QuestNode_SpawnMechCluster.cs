using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace GeologicalLandforms.Patches;

/// <summary>
/// Prevent quests with mech cluster on impassable world tiles.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(QuestNode_SpawnMechCluster))]
internal static class Patch_RimWorld_QuestNode_SpawnMechCluster
{
    [HarmonyPostfix]
    [HarmonyPatch("TestRunInt")]
    private static void TestRunInt(Slate slate, ref bool __result)
    {
        if (__result && slate.Get<Map>("map").TileInfo.hilliness == Hilliness.Impassable)
        {
            __result = false;
        }
    }
}

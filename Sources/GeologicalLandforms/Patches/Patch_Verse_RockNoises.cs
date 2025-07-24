#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RockNoises))]
internal static class Patch_Verse_RockNoises
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RockNoises.Init))]
    private static void Init_Postfix(Map map)
    {
        var tile = map.Tile;
        if (tile.Valid && tile.Layer.IsRootSurface)
        {
            var worldData = Find.World.LandformData();
            if (worldData != null && worldData.TryGet(tile.tileId, out var data) && data.RockTypes != null)
            {
                foreach (var rockNoise in RockNoises.rockNoises)
                {
                    if (data.RockTypes.TryGetValue(rockNoise.rockDef, out var value))
                    {
                        rockNoise.noise = new Add(rockNoise.noise, new Const(value));
                    }
                }
            }
        }
    }
}

#endif

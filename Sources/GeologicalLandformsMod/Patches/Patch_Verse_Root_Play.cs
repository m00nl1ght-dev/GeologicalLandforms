using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Root_Play))]
internal static class Patch_Verse_Root_Play
{
    [HarmonyPostfix]
    [HarmonyPatch("SetupForQuickTestPlay")]
    private static void SetupForQuickTestPlay()
    {
        if (GeologicalLandformsMod.Settings.DevQuickTestOverrideEnabled)
        {
            Find.GameInitData.mapSize = GeologicalLandformsMod.Settings.DevQuickTestOverrideMapSize.Value;

            var startingTile = Find.GameInitData.startingTile;

            var biomeId = GeologicalLandformsMod.Settings.DevQuickTestOverrideBiome.Value;
            var landformId = GeologicalLandformsMod.Settings.DevQuickTestOverrideLandform.Value;

            if (biomeId != "None")
            {
                var biome = DefDatabase<BiomeDef>.GetNamed(biomeId, false);
                if (biome != null)
                {
                    #if RW_1_6_OR_GREATER
                    var nbList = new List<PlanetTile>();
                    #else
                    var nbList = new List<int>();
                    #endif

                    var worldGrid = Find.WorldGrid;

                    worldGrid[startingTile].biome = biome;
                    worldGrid.GetTileNeighbors(startingTile, nbList);

                    foreach (var nb in nbList)
                    {
                        worldGrid[nb].biome = biome;
                    }
                }
            }

            if (landformId != "None")
            {
                var landform = LandformManager.FindById(landformId);
                if (landform != null)
                {
                    var tileInfo = WorldTileInfo.Get(startingTile, false);

                    var tileData = new LandformData.TileData(tileInfo)
                    {
                        Landforms = [landform.Id]
                    };

                    if (landform.WorldTileReq?.HillinessRequirement.max > 5)
                    {
                        Find.WorldGrid[startingTile].hilliness = Hilliness.Impassable;
                    }

                    Find.World.LandformData().Commit(startingTile, tileData);
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.Defs;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace GeologicalLandforms;

public class PocketMapInfo : IWorldTileInfo
{
    public IReadOnlyList<Landform> Landforms { get; private set; }
    public IReadOnlyList<BiomeVariantDef> BiomeVariants { get; private set; }
    public IReadOnlyList<BorderingBiome> BorderingBiomes => null;

    public Topology Topology => default;
    public float TopologyValue => default;
    public Rot4 TopologyDirection => default;
    public byte DepthInCaveSystem => default;
    public StructRot4<CoastType> Coast => default;
    public RiverType RiverType => default;
    public MapParent WorldObject => default;

    public BiomeDef Biome => Tile.biome;
    public Hilliness Hilliness => Tile.hilliness;
    public float Elevation => Tile.elevation;
    public float Temperature => Tile.temperature;
    public float Rainfall => Tile.rainfall;
    public float Swampiness => Tile.swampiness;
    public float Pollution => Tile.pollution;

    public bool HasCaves => false;
    public RiverDef MainRiver => null;
    public RoadDef MainRoad => null;
    public IRiverData Rivers => new RiverData();
    public IRoadData Roads => new RoadData();
    public Vector3 PosInWorld => Vector3.zero;

    public int StableSeed(int salt) => Gen.HashCombineInt(Tile.GetHashCode(), salt);

    internal readonly Tile Tile;

    private PocketMapInfo(Tile tile)
    {
        Tile = tile;
    }

    public static IWorldTileInfo Get(Tile tile, PocketMapProperties mapProps = null)
    {
        var info = new PocketMapInfo(tile);

        List<string> landforms = [];
        List<string> biomeVariants = [];

        var ctxTile = new CtxTile(info);
        var biomeProps = tile.biome.Properties();

        biomeProps.overrideLandforms?.Apply(ctxTile, ref landforms);
        biomeProps.overrideBiomeVariants?.Apply(ctxTile, ref biomeVariants);

        mapProps?.landforms?.Apply(ctxTile, ref landforms);
        mapProps?.biomeVariants?.Apply(ctxTile, ref biomeVariants);

        if (landforms.Count > 0)
            info.Landforms = landforms
                .Select(LandformManager.FindById)
                .Where(lf => lf != null)
                .OrderBy(lf => lf.Priority)
                .ToArray();

        if (biomeVariants.Count > 0)
            info.BiomeVariants = biomeVariants
                .Select(id => DefDatabase<BiomeVariantDef>.GetNamed(id, false))
                .Where(bv => bv != null)
                .ToArray();

        return info;
    }

    public override string ToString() =>
        $"{nameof(Biome)}: {Biome?.defName}, " +
        $"{nameof(Landforms)}: {Landforms?.Join(null, " ") ?? "None"}, " +
        $"{nameof(BiomeVariants)}: {BiomeVariants?.Join(null, " ") ?? "None"}";

    public struct RiverData : IRiverData
    {
        public float RiverInflowAngle => 0f;
        public float RiverInflowOffset => 0f;
        public float RiverInflowWidth => 0f;
        public float RiverTributaryAngle => 0f;
        public float RiverTributaryOffset => 0f;
        public float RiverTributaryWidth => 0f;
        public float RiverTertiaryWidth => 0f;
        public float RiverTertiaryAngle => 0f;
        public float RiverTertiaryOffset => 0f;
        public float RiverOutflowAngle => 0f;
        public float RiverOutflowWidth => 0f;
    }

    public class RoadData : IRoadData
    {
        public float RoadPrimaryAngle => 0f;
        public float RoadSecondaryAngle => 0f;
    }
}

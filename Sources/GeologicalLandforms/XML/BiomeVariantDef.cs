using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LunarFramework.XML;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static GeologicalLandforms.BiomeProperties;

namespace GeologicalLandforms.Defs;

public class BiomeVariantDef : Def
{
    public List<BiomeVariantLayer> layers;

    public XmlDynamicValue<bool, ICtxTile> worldTileConditions;

    public XmlDynamicValue<string, ICtxTile> biomeLabel;
    public XmlDynamicValue<string, ICtxTile> biomeDescription;

    public WorldTileOverrides worldTileOverrides;
    public WorldTileGraphicAtlas worldTileGraphicAtlas;

    [NoTranslate]
    public string texture;

    public bool useOceanMaterial;

    [Unsaved]
    private Material cachedMat;

    public Material DrawMaterial
    {
        get
        {
            if (cachedMat != null) return cachedMat;
            if (texture.NullOrEmpty()) return null;
            cachedMat = useOceanMaterial ? new Material(WorldMaterials.WorldOcean) : new Material(WorldMaterials.WorldTerrain);
            cachedMat.mainTexture = ContentFinder<Texture2D>.Get(texture);
            cachedMat.renderQueue = 3501;
            return cachedMat;
        }
    }

    public string ApplyToBiomeLabel(WorldTileInfo tile, string str)
        => biomeLabel == null ? str : biomeLabel.Get(new CtxTile(tile), str);

    public string ApplyToBiomeDescription(WorldTileInfo tile, string str)
        => biomeDescription == null ? str : biomeDescription.Get(new CtxTile(tile), str);

    private static readonly Regex AllowedIdRegex = new("^[a-zA-Z0-9\\-_]*$");

    public override void PostLoad()
    {
        foreach (var layer in layers)
        {
            layer.Def = this;
            if (string.IsNullOrEmpty(layer.id)) throw new Exception("layer id can not be empty");
            if (!AllowedIdRegex.IsMatch(layer.id)) throw new Exception("layer id " + layer.id + " in invalid");
            if (layers.Any(l => l.id == layer.id && l != layer)) throw new Exception("layer id duplicate: " + layer);
        }
    }

    public static bool AnyHasTileGraphic { get; private set; }

    public static void InitialLoad()
    {
        AnyHasTileGraphic = DefDatabase<BiomeVariantDef>.AllDefs.Any(def => def.DrawMaterial != null || def.worldTileGraphicAtlas != null);

        foreach (var grp in DefDatabase<BiomeVariantDef>.AllDefsListForReading.GroupBy(def => def.modContentPack))
        {
            GeologicalLandformsAPI.Logger.Log($"Found {grp.Count()} biome variants in mod {grp.Key.Name}.");
        }
    }

    public static BiomeVariantDef Named(string defName) => DefDatabase<BiomeVariantDef>.GetNamed(defName);
}

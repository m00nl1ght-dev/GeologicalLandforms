using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld.Planet;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Defs;

[Serializable]
public class BiomeVariantDef : Def
{
    public WorldTileConditions worldTileConditions;
    public List<BiomeVariantLayer> layers;

    public LabelDisplayMode labelDisplayMode = LabelDisplayMode.AppendPara;
    public DescriptionDisplayMode descriptionDisplayMode = DescriptionDisplayMode.Append;

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
            return cachedMat;
        }
    }

    public TaggedString ApplyLabel(TaggedString baseLabel)
    {
        return labelDisplayMode switch
        {
            LabelDisplayMode.None => baseLabel,
            LabelDisplayMode.AppendPara => baseLabel + " (" + label + ")",
            LabelDisplayMode.Append => baseLabel + " " + label,
            LabelDisplayMode.Prepend => LabelCap + " " + baseLabel,
            LabelDisplayMode.PrependPara => LabelCap + " (" + baseLabel.ToLower() + ")",
            LabelDisplayMode.Replace => LabelCap,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string ApplyDescription(string baseDescription)
    {
        return descriptionDisplayMode switch
        {
            DescriptionDisplayMode.None => baseDescription,
            DescriptionDisplayMode.Append => baseDescription + "\n\n" + description,
            DescriptionDisplayMode.Prepend => description + "\n\n" + baseDescription,
            DescriptionDisplayMode.Replace => description,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static Regex AllowedIdRegex = new("^[a-zA-Z0-9\\-_]*$");

    public override void PostLoad()
    {
        foreach (var layer in layers)
        {
            layer.def = this;
            if (string.IsNullOrEmpty(layer.id)) throw new Exception("layer id can not be empty");
            if (!AllowedIdRegex.IsMatch(layer.id)) throw new Exception("layer id " + layer.id + " in invalid");
            if (layers.Any(l => l.id == layer.id && l != layer)) throw new Exception("layer id duplicate: " + layer);
        }
    }

    public static void InitialLoad()
    {
        foreach (var grp in DefDatabase<BiomeVariantDef>.AllDefsListForReading.GroupBy(def => def.modContentPack))
        {
            GeologicalLandformsAPI.Logger.Log($"Found {grp.Count()} biome variants in mod {grp.Key.Name}.");
        }
    }

    public static BiomeVariantDef Named(string defName) => DefDatabase<BiomeVariantDef>.GetNamed(defName);

    public enum LabelDisplayMode
    {
        None,
        Append,
        AppendPara,
        Prepend,
        PrependPara,
        Replace
    }

    public enum DescriptionDisplayMode
    {
        None,
        Append,
        Prepend,
        Replace
    }
}

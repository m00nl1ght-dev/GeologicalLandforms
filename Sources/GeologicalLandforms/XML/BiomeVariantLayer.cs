using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LunarFramework.XML;
using RimWorld;
using Verse;

namespace GeologicalLandforms.Defs;

public class BiomeVariantLayer
{
    [NoTranslate]
    public string id = "main";

    public int priority;

    public XmlDynamicValue<bool, ICtxMapCell> mapGridConditions;

    public XmlDynamicValue<float, ICtxTile> animalDensity;
    public XmlDynamicValue<float, ICtxTile> plantDensity;
    public XmlDynamicValue<float, ICtxTile> wildPlantRegrowDays;

    public XmlListModifier<BiomePlantRecord> wildPlants;
    public XmlListModifier<BiomeAnimalRecord> wildAnimals;
    public XmlListModifier<BiomeAnimalRecord> pollutionWildAnimals;

    public bool applyToCaveSpawns;

    public BiomeVariantDef Def { get; internal set; }

    public override string ToString()
    {
        return Def.defName + "/" + id;
    }

    public static BiomeVariantLayer FindFromString(string defSlashLayerId)
    {
        var parts = defSlashLayerId.Split('/');

        if (parts.Length != 2)
        {
            GeologicalLandformsAPI.Logger.Error("Ignoring invalid biome variant id: " + defSlashLayerId);
            return null;
        }

        var variant = DefDatabase<BiomeVariantDef>.GetNamed(parts[0], false);
        var layer = variant?.layers?.FirstOrDefault(l => l.id == parts[1]);

        if (layer == null)
        {
            GeologicalLandformsAPI.Logger.Error("Ignoring missing biome variant: " + defSlashLayerId);
            return null;
        }

        return layer;
    }

    private static readonly MethodInfo _cloneMethod;
    private static readonly FieldInfo _wildPlantsField;
    private static readonly FieldInfo _wildAnimalsField;
    private static readonly FieldInfo _pollutionWildAnimalsField;
    private static readonly List<FieldInfo> _cacheFields;

    static BiomeVariantLayer()
    {
        _cloneMethod = typeof(BiomeDef).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        _wildPlantsField = typeof(BiomeDef).GetField("wildPlants", BindingFlags.Instance | BindingFlags.NonPublic);
        _wildAnimalsField = typeof(BiomeDef).GetField("wildAnimals", BindingFlags.Instance | BindingFlags.NonPublic);
        _pollutionWildAnimalsField = typeof(BiomeDef).GetField("pollutionWildAnimals", BindingFlags.Instance | BindingFlags.NonPublic);

        _cacheFields = typeof(BiomeDef).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.Name.StartsWith("cached")).ToList();

        if (_cloneMethod == null) throw new Exception("Failed to reflect BiomeDef clone method");
        if (_wildPlantsField == null) throw new Exception("Failed to reflect BiomeDef wildPlants field");
        if (_wildAnimalsField == null) throw new Exception("Failed to reflect BiomeDef wildAnimals field");
        if (_pollutionWildAnimalsField == null) throw new Exception("Failed to reflect BiomeDef pollutionWildAnimals field");
    }

    public static BiomeDef Apply(WorldTileInfo tile, BiomeDef biomeBase, List<BiomeVariantLayer> layers)
    {
        var def = (BiomeDef) _cloneMethod.Invoke(biomeBase, null);
        def.generated = true;
        def.shortHash = 0;

        foreach (var cacheField in _cacheFields) cacheField.SetValue(def, null);

        var wildPlants = (List<BiomePlantRecord>) _wildPlantsField.GetValue(biomeBase);
        var wildAnimals = (List<BiomeAnimalRecord>) _wildAnimalsField.GetValue(biomeBase);
        var pollutionWildAnimals = (List<BiomeAnimalRecord>) _pollutionWildAnimalsField.GetValue(biomeBase);

        _wildPlantsField.SetValue(def, wildPlants = new List<BiomePlantRecord>(wildPlants));
        _wildAnimalsField.SetValue(def, wildAnimals = new List<BiomeAnimalRecord>(wildAnimals));
        _pollutionWildAnimalsField.SetValue(def, pollutionWildAnimals = new List<BiomeAnimalRecord>(pollutionWildAnimals));

        var xmlContext = new CtxTile(tile);

        foreach (var variant in layers)
        {
            variant.animalDensity?.Apply(xmlContext, ref def.animalDensity);
            variant.plantDensity?.Apply(xmlContext, ref def.plantDensity);
            variant.wildPlantRegrowDays?.Apply(xmlContext, ref def.wildPlantRegrowDays);

            variant.wildPlants?.Apply(wildPlants, e => e.plant);
            variant.wildAnimals?.Apply(wildAnimals, e => e.animal);
            variant.pollutionWildAnimals?.Apply(pollutionWildAnimals, e => e.animal);
        }

        return def;
    }
}

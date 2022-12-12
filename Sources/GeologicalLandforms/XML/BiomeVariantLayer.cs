using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming

namespace GeologicalLandforms.Defs;

[Serializable]
public class BiomeVariantLayer
{
    public string id = "main";
    public int priority;

    public BiomeVariantDef def { get; internal set; }

    public MapGridConditions mapGridConditions = new();
    
    public XmlValueModifier animalDensity;
    public XmlValueModifier plantDensity;
    public XmlValueModifier wildPlantRegrowDays;
    
    public XmlListModifier<BiomePlantRecord> wildPlants;
    public XmlListModifier<BiomeAnimalRecord> wildAnimals;
    public XmlListModifier<BiomeAnimalRecord> pollutionWildAnimals;

    public bool applyToCaveSpawns;
    
    public override string ToString()
    {
        return def.defName + "/" + id;
    }

    public static BiomeVariantLayer FindFromString(string defSlashLayerId)
    {
        var parts = defSlashLayerId.Split('/');
        if (parts.Length != 2) throw new Exception("invalid biome variant: " + defSlashLayerId);
        var variant = BiomeVariantDef.Named(parts[0]);
        var layer = variant.layers.FirstOrDefault(l => l.id == parts[1]);
        if (layer == null) throw new Exception("biome variant layer not found: " + defSlashLayerId);
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
    
    public static BiomeDef Apply(BiomeDef biomeBase, List<BiomeVariantLayer> layers)
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
        
        foreach (var variant in layers)
        {
            variant.animalDensity?.Apply(ref def.animalDensity);
            variant.plantDensity?.Apply(ref def.plantDensity);
            variant.wildPlantRegrowDays?.Apply(ref def.wildPlantRegrowDays);

            variant.wildPlants?.Apply(wildPlants, e => e.plant);
            variant.wildAnimals?.Apply(wildAnimals, e => e.animal);
            variant.pollutionWildAnimals?.Apply(pollutionWildAnimals, e => e.animal);
        }

        return def;
    }
}
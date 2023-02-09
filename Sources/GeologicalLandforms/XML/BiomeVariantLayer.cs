using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
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

    public XmlDynamicValue<List<DynamicBiomePlantRecord>, ICtxTile> wildPlants;
    public XmlDynamicValue<List<DynamicBiomeAnimalRecord>, ICtxTile> wildAnimals;
    public XmlDynamicValue<List<DynamicBiomeAnimalRecord>, ICtxTile> pollutionWildAnimals;

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

        var baseWildPlants = (List<BiomePlantRecord>) _wildPlantsField.GetValue(biomeBase);
        var baseWildAnimals = (List<BiomeAnimalRecord>) _wildAnimalsField.GetValue(biomeBase);
        var basePollutionWildAnimals = (List<BiomeAnimalRecord>) _pollutionWildAnimalsField.GetValue(biomeBase);

        var wildPlants = baseWildPlants.Select(r => new DynamicBiomePlantRecord(r)).ToList();
        var wildAnimals = baseWildAnimals.Select(r => new DynamicBiomeAnimalRecord(r)).ToList();
        var pollutionWildAnimals = basePollutionWildAnimals.Select(r => new DynamicBiomeAnimalRecord(r)).ToList();

        var xmlContext = new CtxTile(tile);

        foreach (var variant in layers)
        {
            variant.animalDensity?.Apply(xmlContext, ref def.animalDensity);
            variant.plantDensity?.Apply(xmlContext, ref def.plantDensity);
            variant.wildPlantRegrowDays?.Apply(xmlContext, ref def.wildPlantRegrowDays);

            variant.wildPlants?.Apply(xmlContext, ref wildPlants);
            variant.wildAnimals?.Apply(xmlContext, ref wildAnimals);
            variant.pollutionWildAnimals?.Apply(xmlContext, ref pollutionWildAnimals);
        }
        
        _wildPlantsField.SetValue(def, wildPlants.Select(r => r.Resolve(xmlContext)).ToList());
        _wildAnimalsField.SetValue(def, wildAnimals.Select(r => r.Resolve(xmlContext)).ToList());
        _pollutionWildAnimalsField.SetValue(def, pollutionWildAnimals.Select(r => r.Resolve(xmlContext)).ToList());

        return def;
    }
}

public class DynamicBiomePlantRecord : IEquatable<DynamicBiomePlantRecord>
{
    public ThingDef plant;
    public XmlDynamicValue<float, ICtxTile> commonality;
    
    public DynamicBiomePlantRecord() { }

    public DynamicBiomePlantRecord(BiomePlantRecord record)
    {
        plant = record.plant;
        commonality = new XmlDynamicValue<float, ICtxTile>(record.commonality);
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "plant", xmlRoot);
        commonality = DirectXmlToObject.ObjectFromXml<XmlDynamicValue<float, ICtxTile>>(xmlRoot, false);
    }

    public BiomePlantRecord Resolve(ICtxTile ctx)
    {
        return new BiomePlantRecord { plant = plant, commonality = commonality.Get(ctx) };
    }

    public bool Equals(DynamicBiomePlantRecord other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(plant, other.plant);
    }

    public override int GetHashCode()
    {
        return plant != null ? plant.GetHashCode() : 0;
    }
}

public class DynamicBiomeAnimalRecord : IEquatable<DynamicBiomeAnimalRecord>
{
    public PawnKindDef animal;
    public XmlDynamicValue<float, ICtxTile> commonality;
    
    public DynamicBiomeAnimalRecord() { }

    public DynamicBiomeAnimalRecord(BiomeAnimalRecord record)
    {
        animal = record.animal;
        commonality = new XmlDynamicValue<float, ICtxTile>(record.commonality);
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "animal", xmlRoot);
        commonality = DirectXmlToObject.ObjectFromXml<XmlDynamicValue<float, ICtxTile>>(xmlRoot, false);
    }
    
    public BiomeAnimalRecord Resolve(ICtxTile ctx)
    {
        return new BiomeAnimalRecord { animal = animal, commonality = commonality.Get(ctx) };
    }

    public bool Equals(DynamicBiomeAnimalRecord other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(animal, other.animal);
    }

    public override int GetHashCode()
    {
        return animal != null ? animal.GetHashCode() : 0;
    }
}

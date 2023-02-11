using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using LunarFramework.Utility;
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

    public static BiomeDef Apply(WorldTileInfo tile, BiomeDef biomeBase, List<BiomeVariantLayer> layers)
    {
        var def = biomeBase.MakeShallowCopy();

        def.shortHash = 0;
        def.generated = true;

        def.cachedWildPlants = null;
        def.cachedPlantCommonalities = null;
        def.cachedAnimalCommonalities = null;
        def.cachedPollutionAnimalCommonalities = null;
        def.cachedDiseaseCommonalities = null;
        def.cachedMaxWildPlantsClusterRadius = null;
        def.cachedLowestWildPlantOrder = null;

        var wildPlants = def.wildPlants.Select(r => new DynamicBiomePlantRecord(r)).ToList();
        var wildAnimals = def.wildAnimals.Select(r => new DynamicBiomeAnimalRecord(r)).ToList();
        var pollutionWildAnimals = def.pollutionWildAnimals.Select(r => new DynamicBiomeAnimalRecord(r)).ToList();

        var ctx = new CtxTile(tile);

        foreach (var layer in layers)
        {
            layer.animalDensity?.Apply(ctx, ref def.animalDensity);
            layer.plantDensity?.Apply(ctx, ref def.plantDensity);
            layer.wildPlantRegrowDays?.Apply(ctx, ref def.wildPlantRegrowDays);

            layer.wildPlants?.Apply(ctx, ref wildPlants);
            layer.wildAnimals?.Apply(ctx, ref wildAnimals);
            layer.pollutionWildAnimals?.Apply(ctx, ref pollutionWildAnimals);
        }

        def.wildPlants = wildPlants.Select(r => r.Resolve(ctx)).ToList();
        def.wildAnimals = wildAnimals.Select(r => r.Resolve(ctx)).ToList();
        def.pollutionWildAnimals = pollutionWildAnimals.Select(r => r.Resolve(ctx)).ToList();

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

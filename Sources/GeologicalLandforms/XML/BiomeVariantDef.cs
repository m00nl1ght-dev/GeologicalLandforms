using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

// ReSharper disable UnusedMember.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace GeologicalLandforms;

public class BiomeVariantDef : Def
{
    public float animalDensity;
    public float plantDensity;
    
    public bool wildPlantsCareAboutLocalFertility = true;
    public float wildPlantRegrowDays = 25f;
    
    private List<BiomePlantRecord> wildPlants = new();
    private List<BiomeAnimalRecord> wildAnimals = new();
    private List<BiomeAnimalRecord> pollutionWildAnimals = new();
    
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
    
    public static BiomeDef ApplyVariants(BiomeDef biomeBase, List<BiomeVariantDef> biomeVariants)
    {
        // TODO
        return biomeBase;
    }

    public static BiomeVariantDef Named(string defName) => DefDatabase<BiomeVariantDef>.GetNamed(defName);
    
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string configError in base.ConfigErrors()) yield return configError;
        
        if (Prefs.DevMode)
        {
            foreach (var wa in wildAnimals.Where(wa => wildAnimals.Count(a => a.animal == wa.animal) > 1))
            {
                yield return "Duplicate animal record: " + wa.animal.defName;
            }

            if (ModsConfig.BiotechActive)
            {
                foreach (var pa in pollutionWildAnimals.Where(pa => pollutionWildAnimals.Count(a => a.animal == pa.animal) > 1))
                {
                    yield return "Duplicate pollution animal record: " + pa.animal.defName;
                }
            }
        }
    }
}
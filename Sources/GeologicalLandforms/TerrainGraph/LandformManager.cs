using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.IO;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

public static class LandformManager
{
    public static string DefaultLandformsDir => Path.Combine(ModInstance.ModContentPack.RootDir, "LandformData");
    public static string LegacyCustomLandformsDir => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandformData");
    public static string CustomLandformsDir => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandforms");
    
    private static Dictionary<string, Landform> _defaultLandforms = new();
    private static Dictionary<string, Landform> _customLandforms = new();

    public static IReadOnlyDictionary<string, Landform> DefaultLandforms => _defaultLandforms;
    public static IReadOnlyDictionary<string, Landform> CustomLandforms => _customLandforms;

    public static IReadOnlyDictionary<string, Landform> Landforms => ModInstance.Settings?.UseCustomConfig ?? false ? _customLandforms : _defaultLandforms;
    
    private static ImportExportFormat IOFormat => ImportExportManager.ParseFormat("XML");

    public static void InitialLoad()
    {
        Directory.CreateDirectory(CustomLandformsDir);
        _defaultLandforms = LoadLandformsFromDirectory(DefaultLandformsDir, null);
        _customLandforms = LoadLandformsFromDirectory(CustomLandformsDir, _defaultLandforms);
    }
    
    public static void SaveAllCustom()
    {
        SaveLandformsToDirectory(CustomLandformsDir, _customLandforms);
    }

    public static Landform Duplicate(Landform landform)
    {
        SaveAllCustom();
        
        Landform duplicate = LoadLandformsFromDirectory(CustomLandformsDir, _defaultLandforms).TryGetValue(landform.Manifest.Id);
        if (duplicate == null) return null;
        
        string newId = landform.Manifest.Id + "Copy";
        while (CustomLandforms.ContainsKey(newId) || DefaultLandforms.ContainsKey(newId))
        {
            newId += "Copy";
        }
        
        duplicate.Manifest.IsCustom = true;
        duplicate.Manifest.Id = newId;
        
        _customLandforms.Add(newId, duplicate);
        return duplicate;
    }
    
    public static void Rename(Landform landform, string newId)
    {
        if (_customLandforms.ContainsKey(newId))
        {
            newId = "New" + newId;
        }
        
        if (landform.Manifest.Id != null) _customLandforms.Remove(landform.Manifest.Id);
        landform.Manifest.Id = newId;
        _customLandforms.Add(landform.Manifest.Id, landform);
    }

    public static void Delete(Landform landform)
    {
        _customLandforms.Remove(landform.Manifest.Id);
    }
    
    public static Landform Reset(Landform landform)
    {
        Landform reset = LoadLandformsFromDirectory(DefaultLandformsDir, null).TryGetValue(landform.Id);
        if (reset == null) return landform;

        _customLandforms[landform.Id] = reset;
        return reset;
    }
    
    public static void ResetAll()
    {
        _customLandforms = LoadLandformsFromDirectory(DefaultLandformsDir, null);
    }
    
    public static Dictionary<string, Landform> LoadLandformsFromDirectory(string directory, Dictionary<string, Landform> fallback)
    {
        Dictionary<string, Landform> landforms = new(fallback ?? new());
        
        foreach (string file in Directory.GetFiles(directory, "*.xml"))
        {
            Landform landform = null;
            
            try
            {
                if (File.Exists(file))
                {
                    landform = (Landform) ImportExportManager.ImportCanvas(IOFormat, file);
                }

                if (landform != null && landform.Id != null)
                {
                    if (landforms.ContainsKey(landform.Id)) landforms[landform.Id] = landform;
                    else landforms.Add(landform.Id, landform);
                    Log.Message($"Loaded landform {landform.Id} from file {file}.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Caught exception while loading landform from file {file}. The exception was: {ex}");
            }
        }

        if (landforms.Count == 0 && fallback != null)
        {
            Landform landform = NodeCanvas.CreateCanvas<Landform>();
            landform.Manifest.Id = "NewLandform";
            landform.Manifest.DisplayName = "NewLandform";
            landform.Manifest.IsCustom = true;
            landforms.Add(landform.Id, landform);
        }

        return landforms;
    }
    
    public static void SaveLandformsToDirectory(string directory, Dictionary<string, Landform> landforms)
    {
        List<string> existing = Directory.GetFiles(directory, "*.xml").ToList();

        foreach (KeyValuePair<string, Landform> pair in landforms.Where(p => p.Key?.Length > 0))
        {
            string file = Path.Combine(directory, pair.Key + ".xml");
            Landform landform = pair.Value;
            existing.Remove(file);
            
            ImportExportManager.ExportCanvas(landform, IOFormat, file);
        }
        
        foreach (string file in existing)
        {
            File.Delete(file);
        }
    }
}
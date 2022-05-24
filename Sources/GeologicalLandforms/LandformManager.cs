using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeologicalLandforms.Patches;
using NodeEditorFramework;
using NodeEditorFramework.IO;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public static class LandformManager
{
    public const int CurrentVersion = 1;
    
    public static string CoreLandformsDir => Path.Combine(ModInstance.ModContentPack.RootDir, "LandformData");
    
    public static string CustomLandformsDir(int version) => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandforms-v" + version);
    public static string LegacyCustomLandformsDir => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandformData");
    
    private static Dictionary<string, Landform> _landforms = new();
    public static IReadOnlyDictionary<string, Landform> Landforms => _landforms;
    
    private static ImportExportFormat IOFormat => ImportExportManager.ParseFormat("XML");

    public static void InitialLoad()
    {
        Directory.CreateDirectory(CustomLandformsDir(CurrentVersion));
        _landforms = LoadAll();
    }

    public static Dictionary<string, Landform> LoadAll(string fileFilter = "*")
    {
        Dictionary<string, Landform> coreLandforms = LoadLandformsFromDirectory(CoreLandformsDir, null, fileFilter);
        Dictionary<string, Landform> mergedLandforms = LoadLandformsFromDirectory(CustomLandformsDir(CurrentVersion), coreLandforms, fileFilter);

        List<Landform> upgradableLandforms = coreLandforms.Values
            .Where(lf => mergedLandforms[lf.Id].Manifest.RevisionVersion < lf.Manifest.RevisionVersion)
            .Select(lf => mergedLandforms[lf.Id]).ToList();

        if (upgradableLandforms.Count > 0) RimWorld_Misc.OnMainMenu(() =>
        {
            string msg = "GeologicalLandforms.LandformManager.LandformUpgrade".Translate() + "\n";
            msg = upgradableLandforms.Aggregate(msg, (current, lf) => current + ("\n" + lf.TranslatedNameForSelection.CapitalizeFirst()));

            void UpgradeAction()
            {
                foreach (Landform lf in upgradableLandforms) Reset(lf);
                SaveAllEdited();
            }
            
            void KeepAction()
            {
                foreach (Landform lf in upgradableLandforms) lf.Manifest.RevisionVersion += 1;
                SaveAllEdited();
            }

            Find.WindowStack.Add(new Dialog_MessageBox(msg, 
                "GeologicalLandforms.LandformManager.LandformUpgradeYes".Translate(), UpgradeAction,
                "GeologicalLandforms.LandformManager.LandformUpgradeNo".Translate(), KeepAction));
        });
        
        return mergedLandforms;
    }
    
    public static void SaveAllEdited()
    {
        SaveLandformsToDirectory(CustomLandformsDir(CurrentVersion), _landforms);
    }

    public static Landform Duplicate(Landform landform)
    {
        SaveAllEdited();
        
        Landform duplicate = LoadAll("*" + landform.Id).TryGetValue(landform.Manifest.Id);
        duplicate ??= LoadAll().TryGetValue(landform.Manifest.Id);
        if (duplicate == null) return null;
        
        string newId = landform.Manifest.Id + "Copy";
        while (Landforms.ContainsKey(newId))
        {
            newId += "Copy";
        }
        
        duplicate.Manifest.IsCustom = true;
        duplicate.Manifest.IsEdited = true;
        duplicate.Manifest.Id = newId;
        
        _landforms.Add(newId, duplicate);
        return duplicate;
    }
    
    public static void Rename(Landform landform, string newId)
    {
        while (_landforms.ContainsKey(newId))
        {
            newId = "New" + newId;
        }
        
        if (landform.Manifest.Id != null) _landforms.Remove(landform.Manifest.Id);
        landform.Manifest.Id = newId;
        _landforms.Add(landform.Manifest.Id, landform);
    }

    public static void Delete(Landform landform)
    {
        _landforms.Remove(landform.Manifest.Id);
    }
    
    public static Landform Reset(Landform landform)
    {
        Landform reset = LoadLandformsFromDirectory(CoreLandformsDir, null, "*" + landform.Id).TryGetValue(landform.Id);
        reset ??= LoadLandformsFromDirectory(CoreLandformsDir, null).TryGetValue(landform.Id);
        if (reset == null) return landform;

        _landforms[landform.Id] = reset;
        return reset;
    }
    
    public static void ResetAll()
    {
        _landforms = LoadLandformsFromDirectory(CoreLandformsDir, null);
    }
    
    public static Dictionary<string, Landform> LoadLandformsFromDirectory(string directory, Dictionary<string, Landform> fallback, string fileFilter = "*")
    {
        Dictionary<string, Landform> landforms = new(fallback ?? new());
        
        foreach (string file in Directory.GetFiles(directory, fileFilter + ".xml"))
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
                    // Log.Message($"Loaded landform {landform.Id} from file {file}.");
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

        foreach (KeyValuePair<string, Landform> pair in landforms.Where(p => true)) // TODO revert
        {
            string file = Path.Combine(directory, "Landform" + pair.Key + ".xml");
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
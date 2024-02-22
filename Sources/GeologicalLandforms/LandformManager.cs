using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using NodeEditorFramework;
using NodeEditorFramework.IO;
using RimWorld;
using Verse;
using static GeologicalLandforms.GeologicalLandformsAPI;

namespace GeologicalLandforms.GraphEditor;

public static class LandformManager
{
    public const int CurrentVersion = 1;

    public static IReadOnlyDictionary<string, Landform> LandformsById => _landformsById;

    public static IReadOnlyList<Layer> LandformLayers { get; private set; }

    public static bool AnyHasTileGraphic { get; private set; }

    private static Dictionary<string, Landform> _landformsById = new();
    private static Dictionary<string, ModContentPack> _landformDirs = new();

    private static ImportExportFormat IOFormat => ImportExportManager.ParseFormat("XML");
    private static readonly IReadOnlyList<string> KnownInternalLandforms = ["BiomeTransitions"];

    public static void InitialLoad()
    {
        NodeEditor.checkInit(false);

        Directory.CreateDirectory(CustomLandformsDir(CurrentVersion));

        _landformDirs = new();

        foreach (var mcp in LoadedModManager.RunningMods)
        {
            if (mcp?.RootDir == null) continue;

            foreach (var dir in mcp.foldersToLoadDescendingOrder
                .SelectMany(loadFolder => LandformDirs(loadFolder, CurrentVersion))
                .Reverse().Where(Directory.Exists))
            {
                _landformDirs.Add(dir, mcp);
            }
        }

        Logger.Log("Found landform data in the following mods: " + _landformDirs.Values.Distinct().Join());

        _landformsById = LoadAll();

        RefreshLayers();
    }

    internal static void RefreshLayers()
    {
        LandformLayers = _landformsById.Values
            .GroupBy(lf => lf.IsLayer ? lf.LayerConfig.LayerId : null)
            .Select(g => new Layer(g.Key, g.OrderBy(lf => lf.Manifest.TimeCreated).ToArray()))
            .ToArray();

        AnyHasTileGraphic = _landformsById.Values.Any(lf => lf.WorldTileGraphic != null);
    }

    public static Dictionary<string, Landform> LoadAll(string fileFilter = "*", bool includeCustom = true)
    {
        var mcpLandforms = _landformDirs.Aggregate<KeyValuePair<string, ModContentPack>, Dictionary<string, Landform>>(null,
            (current, source) => LoadLandformsFromDirectory(source.Key, current, fileFilter, source.Value));

        foreach (var landform in mcpLandforms.Values)
        {
            landform.Manifest.IsEdited = false;
            landform.Manifest.IsCustom = false;
        }

        if (!includeCustom) return mcpLandforms;

        var mergedLandforms = LoadLandformsFromDirectory(CustomLandformsDir(CurrentVersion), mcpLandforms, fileFilter);
        var upgradableLandforms = mcpLandforms.Values
            .Where(lf => mergedLandforms[lf.Id].Manifest.RevisionVersion < lf.Manifest.RevisionVersion)
            .Select(lf => mergedLandforms[lf.Id]).ToList();

        int customCount = mergedLandforms.Values.Count(l => l.IsCustom);
        int editedCount = mergedLandforms.Values.Count(l => l.Manifest.IsEdited);

        Logger.Log("Loaded " + mergedLandforms.Count + " landforms of which " + editedCount + " are edited and " + customCount + " are custom.");

        if (upgradableLandforms.Count > 0)
        {
            LunarAPI.LifecycleHooks.DoOnceOnMainMenu(() =>
            {
                string msg = "GeologicalLandforms.LandformManager.LandformUpgrade".Translate() + "\n";
                msg = upgradableLandforms.Aggregate(msg, (current, lf) => current + ("\n" + lf.TranslatedNameForSelection.CapitalizeFirst()));

                void UpgradeAction()
                {
                    foreach (var lf in upgradableLandforms) Reset(lf);
                    SaveAllEdited();
                }

                void KeepAction()
                {
                    foreach (var lf in upgradableLandforms) lf.Manifest.RevisionVersion += 1;
                    SaveAllEdited();
                }

                Find.WindowStack.Add(new Dialog_MessageBox(msg,
                    "GeologicalLandforms.LandformManager.LandformUpgradeYes".Translate(), UpgradeAction,
                    "GeologicalLandforms.LandformManager.LandformUpgradeNo".Translate(), KeepAction));
            });
        }

        return mergedLandforms;
    }

    public static void SaveAllEdited()
    {
        SaveLandformsToDirectory(CustomLandformsDir(CurrentVersion), _landformsById);
    }

    public static Landform FindById(string id)
    {
        return LandformsById.TryGetValue(id, out var landform) ? landform : null;
    }

    public static Landform Duplicate(Landform landform)
    {
        SaveAllEdited();

        var duplicate = LoadAll("*" + landform.Id).TryGetValue(landform.Manifest.Id);
        duplicate ??= LoadAll().TryGetValue(landform.Manifest.Id);
        if (duplicate == null) return null;

        string newId = landform.Manifest.Id + "Copy";
        while (LandformsById.ContainsKey(newId))
        {
            newId += "Copy";
        }

        duplicate.Manifest.IsCustom = true;
        duplicate.Manifest.IsEdited = true;
        duplicate.Manifest.TimeCreated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        duplicate.Manifest.Id = newId;
        duplicate.ModContentPack = null;
        duplicate.OriginalFileLocation = null;

        _landformsById.Add(newId, duplicate);

        RefreshLayers();

        return duplicate;
    }

    public static void Rename(Landform landform, string newId)
    {
        while (_landformsById.ContainsKey(newId))
        {
            newId = "New" + newId;
        }

        if (landform.Manifest.Id != null) _landformsById.Remove(landform.Manifest.Id);
        landform.Manifest.Id = newId;
        _landformsById.Add(landform.Manifest.Id, landform);
    }

    public static void SaveInMod(Landform landform)
    {
        var file = landform.OriginalFileLocation;
        var dir = Path.GetDirectoryName(file);
        if (dir == null) return;

        landform.Manifest.IsEdited = false;
        landform.Manifest.IsCustom = false;
        landform.Manifest.RevisionVersion++;

        landform.ResetView();

        try
        {
            Directory.CreateDirectory(dir);
            ImportExportManager.ExportCanvas(landform, IOFormat, file);

            var msg = "GeologicalLandforms.Editor.SaveInMod.Success".Translate(file);
            Messages.Message(msg, MessageTypeDefOf.SilentInput, false);
        }
        catch (Exception e)
        {
            Logger.Warn("Failed to save landform in " + file, e);
            landform.Manifest.IsEdited = true;

            var msg = "GeologicalLandforms.Editor.SaveInMod.Error".Translate(file);
            Messages.Message(msg, MessageTypeDefOf.RejectInput, false);
        }
    }

    public static void Delete(Landform landform)
    {
        _landformsById.Remove(landform.Manifest.Id);
        RefreshLayers();
    }

    public static Landform Reset(Landform landform)
    {
        var reset = LoadAll("*" + landform.Id, false).TryGetValue(landform.Id);
        reset ??= LoadAll("*", false).TryGetValue(landform.Id);
        if (reset == null) return landform;

        _landformsById[landform.Id] = reset;
        RefreshLayers();

        return reset;
    }

    public static void ResetAll()
    {
        _landformsById = LoadAll("*", false);
        RefreshLayers();
    }

    public static Dictionary<string, Landform> LoadLandformsFromDirectory(string directory, Dictionary<string, Landform> fallback, string fileFilter = "*", ModContentPack source = null)
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
                    if (KnownInternalLandforms.Contains(landform.Id))
                    {
                        landform.Manifest.IsInternal = true;
                    }

                    if (landforms.TryGetValue(landform.Id, out var existing))
                    {
                        landforms[landform.Id] = landform;

                        if (source != null)
                        {
                            landform.ModContentPack = source;
                            landform.OriginalFileLocation = file;
                        }
                        else
                        {
                            landform.ModContentPack = existing.ModContentPack;
                            landform.OriginalFileLocation = existing.OriginalFileLocation;
                        }
                    }
                    else
                    {
                        landforms.Add(landform.Id, landform);

                        if (source != null)
                        {
                            landform.ModContentPack = source;
                            landform.OriginalFileLocation = file;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Caught exception while loading landform from file {file}.", ex);
            }
        }

        if (landforms.Count == 0 && fallback != null)
        {
            var landform = NodeCanvas.CreateCanvas<Landform>();
            landform.Manifest.Id = "NewLandform";
            landform.Manifest.DisplayName = "NewLandform";
            landform.Manifest.IsCustom = true;
            landforms.Add(landform.Id, landform);
        }

        return landforms;
    }

    public static void SaveLandformsToDirectory(string directory, Dictionary<string, Landform> landforms)
    {
        try
        {
            var existing = Directory.GetFiles(directory, "*.xml").ToList();

            foreach (var pair in landforms.Where(p => p.Value.Manifest.IsEdited || p.Value.Manifest.IsCustom))
            {
                string file = Path.Combine(directory, "Landform" + pair.Key + ".xml");
                var landform = pair.Value;
                existing.Remove(file);

                ImportExportManager.ExportCanvas(landform, IOFormat, file);
            }

            foreach (string file in existing)
            {
                var backupDir = Path.Combine(directory, "Backup");
                var dest = Path.Combine(backupDir, DateTime.Now.ToString("yyyyMMddTHHmmss") + "-" + Path.GetFileName(file));
                Directory.CreateDirectory(backupDir);
                File.Move(file, dest);
            }
        }
        catch (Exception e)
        {
            Logger.Error("Failed to save landforms", e);
        }
    }

    public static IEnumerable<string> LandformDirs(string loadFolder, int version)
    {
        yield return Path.Combine(loadFolder, "Landforms-v" + version);
        yield return Path.Combine(loadFolder, "Landforms");
    }

    public static string CustomLandformsDir(int version)
    {
        return Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandforms-v" + version);
    }

    public readonly struct Layer
    {
        public readonly string LayerId;
        public readonly int SelectionSeed;

        public readonly IReadOnlyList<Landform> Landforms;

        public Layer(string layerId, IReadOnlyList<Landform> landforms)
        {
            LayerId = layerId;
            SelectionSeed = layerId == null ? 1754 : GenText.StableStringHash(layerId);
            Landforms = landforms;
        }
    }
}

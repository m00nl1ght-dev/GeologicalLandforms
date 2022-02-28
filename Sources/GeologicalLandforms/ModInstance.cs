using System.IO;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class ModInstance : Mod
{
    public static ModContentPack ModContentPack;
    public static string DefaultLandformsDir => Path.Combine(ModContentPack.RootDir, "LandformData");
    public static string CustomLandformsDir => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandformData");
    
    public ModInstance(ModContentPack content) : base(content)
    {
        ModContentPack = content;
        Main.Settings = GetSettings<Settings>();
        Directory.CreateDirectory(CustomLandformsDir);
        Main.Settings.DefaultLandforms = Settings.LoadLandformsFromDirectory(DefaultLandformsDir, null);
        Main.Settings.CustomLandforms = Settings.LoadLandformsFromDirectory(CustomLandformsDir, Main.Settings.DefaultLandforms);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Main.Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Geological Landforms";

    public override void WriteSettings()
    {
        base.WriteSettings();
        if (Main.Settings.CustomDataModified)
        {
            Settings.SaveLandformsToDirectory(CustomLandformsDir, Main.Settings.CustomLandforms);
            Main.Settings.CustomDataModified = false;
        }
    }
}
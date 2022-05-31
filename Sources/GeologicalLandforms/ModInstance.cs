using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class ModInstance : Mod
{
    public const string Version = "1.3.2";

    public static string LogPrefix => "[Geological Landforms v" + Version + "] ";
    
    public static ModContentPack ModContentPack;
    public static Settings Settings;

    public ModInstance(ModContentPack content) : base(content)
    {
        ModContentPack = content;
        Settings = GetSettings<Settings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Geological Landforms";
}
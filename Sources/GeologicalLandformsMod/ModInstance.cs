using System;
using UnityEngine;
using Verse;

namespace GeologicalLandforms;

public class ModInstance : Mod
{
    public static readonly Version RequiredLibVersion = new("1.5.0");
    
    public static Version Version => typeof(ModInstance).Assembly.GetName().Version;
    public static Version LibVersion => typeof(EventHooks).Assembly.GetName().Version;

    public static ModContentPack ModContentPack;
    public static Settings Settings;

    static ModInstance()
    {
        if (LibVersion.CompareTo(RequiredLibVersion) < 0)
        {
            var msg =
            $"Geological Landforms v{Version} found incompatible lib version v{LibVersion}. " +
            $"Please fix your mod load order or auto-sort your mod list.";
            Log.Error(msg);
        }
    }

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
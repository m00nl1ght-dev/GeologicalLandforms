using System;
using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms;

public class LandformGenOverrides
{
    private static readonly Dictionary<string, Dictionary<string, string>> Entries = [];

    public void WriteTo(Landform landform)
    {
        if (Entries.TryGetValue(landform.Id, out var entries))
        {
            foreach (var config in landform.ConfigurableOverrides)
            {
                if (!config.PropertyId.NullOrEmpty() && entries.TryGetValue(config.PropertyId, out var value))
                {
                    config.OverrideEnabled = true;
                    SetValueFromString(config, value);
                }
            }
        }
    }

    public void ReadFrom(Landform landform)
    {
        var entries = new Dictionary<string, string>();

        foreach (var config in landform.ConfigurableOverrides)
        {
            if (!config.PropertyId.NullOrEmpty() && config.OverrideEnabled)
            {
                entries[config.PropertyId] = GetValueAsString(config);
            }
        }

        Entries[landform.Id] = entries;
    }

    private static void SetValueFromString(ConfigurableOverride config, string str)
    {
        config.OverrideValue = str == null ? null : config.KnobType switch
        {
            ValueFunctionConnection.Id => double.Parse(str),
            TerrainFunctionConnection.Id => DefDatabase<TerrainDef>.GetNamedSilentFail(str),
            BiomeFunctionConnection.Id => DefDatabase<BiomeDef>.GetNamedSilentFail(str),
            RoofFunctionConnection.Id => DefDatabase<RoofDef>.GetNamedSilentFail(str),
            _ => throw new Exception($"Unsupported value type: {config.KnobType}")
        };
    }

    private static string GetValueAsString(ConfigurableOverride config)
    {
        var value = config.OverrideValue;

        return config.KnobType switch
        {
            ValueFunctionConnection.Id => value is double val ? val.ToString() : null,
            TerrainFunctionConnection.Id => value is TerrainDef def ? def.defName : null,
            BiomeFunctionConnection.Id => value is BiomeDef def ? def.defName : null,
            RoofFunctionConnection.Id => value is RoofDef def ? def.defName : null,
            _ => throw new Exception($"Unsupported value type: {config.KnobType}")
        };
    }
}

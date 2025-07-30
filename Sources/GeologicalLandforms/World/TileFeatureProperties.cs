using System;
using System.Collections.Generic;
using System.Globalization;
using GeologicalLandforms.GraphEditor;
using LunarFramework.Utility;
using RimWorld;
using TerrainGraph;
using Verse;

namespace GeologicalLandforms;

public class TileFeatureProperties : IExposable, IEquatable<TileFeatureProperties>
{
    public Dictionary<string, string> Properties = [];

    public TileFeatureProperties() {}

    public TileFeatureProperties(TileFeatureProperties other)
    {
        Properties = new Dictionary<string, string>(other.Properties);
    }

    public void ApplyToLandform(Landform landform)
    {
        foreach (var config in landform.ConfigurableOverrides)
        {
            if (!config.PropertyId.NullOrEmpty() && Properties.TryGetValue(config.PropertyId, out var value))
            {
                config.OverrideEnabled = true;
                SetValueFromString(config, value);
            }
        }
    }

    public void ReadFromLandform(Landform landform)
    {
        Properties.Clear();

        foreach (var config in landform.ConfigurableOverrides)
        {
            if (!config.PropertyId.NullOrEmpty() && config.OverrideEnabled)
            {
                Properties[config.PropertyId] = GetValueAsString(config);
            }
        }
    }

    private static void SetValueFromString(ConfigurableOverride config, string str)
    {
        config.OverrideValue = str == null ? null : config.KnobType switch
        {
            ValueFunctionConnection.Id => double.Parse(str, CultureInfo.InvariantCulture),
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
            ValueFunctionConnection.Id => value is double val ? val.ToString(CultureInfo.InvariantCulture) : null,
            TerrainFunctionConnection.Id => value is TerrainDef def ? def.defName : null,
            BiomeFunctionConnection.Id => value is BiomeDef def ? def.defName : null,
            RoofFunctionConnection.Id => value is RoofDef def ? def.defName : null,
            _ => throw new Exception($"Unsupported value type: {config.KnobType}")
        };
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref Properties, "properties", LookMode.Value, LookMode.Value);
    }

    public bool Equals(TileFeatureProperties other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Properties.DictEquals(other.Properties);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TileFeatureProperties) obj);
    }

    public override int GetHashCode()
    {
        return Properties.DictHashCode();
    }
}

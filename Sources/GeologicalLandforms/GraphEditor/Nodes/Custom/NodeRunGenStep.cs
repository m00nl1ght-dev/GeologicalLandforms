using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using LunarFramework.Utility;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.GraphEditor;

[HotSwappable]
[Serializable]
[Node(false, "Custom/Run GenStep", 2000)]
public class NodeRunGenStep : NodeBase
{
    public const string ID = "runGenStep";
    public override string GetID => ID;

    public override string Title => "Run GenStep";

    public Landform Landform => (Landform) canvas;

    public string GenStepTypeName;

    public double Order = 230;

    [XmlIgnore]
    public GenStepDef GenStepDef { get; private set; }

    public IEnumerable<ValueConnectionKnob> FieldKnobs => dynamicConnectionPorts.Cast<ValueConnectionKnob>();

    public override void NodeGUI()
    {
        GUILayout.BeginVertical(BoxStyle);

        ValueField("Order", ref Order);

        GUILayout.BeginHorizontal(BoxStyle);

        if (GUILayout.Button(ShortTypeName(GenStepTypeName), GUI.skin.box, FullBoxLayout))
        {
            Dropdown(typeof(GenStep).AllSubclassesNonAbstract(), OnGenStepTypeSelected, t => t.FullName);
        }

        GUILayout.EndHorizontal();

        foreach (var knob in FieldKnobs)
        {
            GUILayout.BeginHorizontal(BoxStyle);

            GUILayout.Label(knob.name, DoubleBoxLayout);
            knob.SetPosition();

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    private void OnGenStepTypeSelected(Type type)
    {
        GenStepTypeName = type.FullName;

        foreach (var port in dynamicConnectionPorts.ToList()) DeleteConnectionPort(port);

        var fields = type.GetFields();

        foreach (var field in fields)
        {
            if (field.Name == "def") continue;
            if (field.IsStatic || field.IsLiteral) continue;

            var connectionId = GetConnectionIdForType(field.FieldType);

            if (connectionId != null)
            {
                CreateValueConnectionKnob(new(field.Name, Direction.In, connectionId));
            }
        }

        canvas.OnNodeChange(this);
    }

    private string GetConnectionIdForType(Type type)
    {
        if (type == typeof(double)) return ValueFunctionConnection.Id;
        if (type == typeof(float)) return ValueFunctionConnection.Id;
        if (type == typeof(int)) return ValueFunctionConnection.Id;
        if (type == typeof(bool)) return ValueFunctionConnection.Id;
        if (type == typeof(TerrainDef)) return TerrainFunctionConnection.Id;
        if (type == typeof(BiomeDef)) return BiomeFunctionConnection.Id;
        if (type == typeof(RoofDef)) return RoofFunctionConnection.Id;
        if (type == typeof(ModuleBase)) return GridFunctionConnection.Id;
        return null;
    }

    private object GetFieldValue(ValueConnectionKnob knob, Type type)
    {
        if (type == typeof(double)) return SupplierOrFixed<double>(knob, 0).ResetAndGet();
        if (type == typeof(float)) return (float) SupplierOrFixed<double>(knob, 0).ResetAndGet();
        if (type == typeof(int)) return (int) SupplierOrFixed<double>(knob, 0).ResetAndGet();
        if (type == typeof(bool)) return SupplierOrFixed<double>(knob, 0).ResetAndGet() > 0f;
        if (type == typeof(TerrainDef)) return SupplierOrFixed(knob, TerrainData.Empty).ResetAndGet().Terrain;
        if (type == typeof(BiomeDef)) return SupplierOrFixed(knob, BiomeData.Empty).ResetAndGet().Biome;
        if (type == typeof(RoofDef)) return SupplierOrFixed(knob, RoofData.Empty).ResetAndGet().Roof;
        if (type == typeof(ModuleBase)) return SupplierOrFixed(knob, GridFunction.Zero).ResetAndGet().AsModule();
        return null;
    }

    private string ShortTypeName(string fullName)
    {
        var idx = fullName.IndexOf(".GenStep_", StringComparison.Ordinal);
        return idx < 0 ? fullName : fullName.Substring(idx + 9);
    }

    public override bool Calculate()
    {
        GenStepDef = null;

        if (GenStepTypeName.NullOrEmpty()) return true;

        try
        {
            var type = GenTypes.GetTypeInAnyAssembly(GenStepTypeName);
            if (type != null)
            {
                var instance = (GenStep) Activator.CreateInstance(type);
                var fields = type.GetFields();
                var knobs = FieldKnobs.ToList();

                foreach (var field in fields)
                {
                    var knob = knobs.FirstOrDefault(k => k.name == field.Name);
                    if (knob == null || !knob.connected()) continue;

                    var value = GetFieldValue(knob, field.FieldType);
                    field.SetValue(instance, value);
                }

                GenStepDef = instance.def = new GenStepDef
                {
                    defName = Landform.Id + "_" + type.Name,
                    order = (float) Order,
                    genStep = instance,
                    generated = true
                };
            }
            else
            {
                GeologicalLandformsAPI.Logger.Log("Skipping missing GenStep: " + GenStepTypeName);
                return true;
            }
        }
        catch (Exception e)
        {
            GeologicalLandformsAPI.Logger.Warn("Failed to build GenStep: " + GenStepTypeName, e);
        }

        return true;
    }

    public override void OnCreate(bool fromGUI)
    {
        Landform.CustomGenSteps.AddDistinct(this);
    }

    protected override void OnDelete()
    {
        Landform.CustomGenSteps.Remove(this);
    }
}

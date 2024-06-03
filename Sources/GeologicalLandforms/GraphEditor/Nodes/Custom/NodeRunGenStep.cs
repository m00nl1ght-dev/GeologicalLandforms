using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using NodeEditorFramework;
using RimWorld;
using TerrainGraph;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Other/Run GenStep", 10000)]
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
            Dropdown(AvailableGenStepClasses, OnGenStepTypeSelected, t => t.FullName);
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

    private static readonly IReadOnlyList<string> IgnoredAssemblies = ["MapPreview"];

    private static List<Type> AvailableGenStepClasses => typeof(GenStep).AllSubclassesNonAbstract()
        .Where(t => !IgnoredAssemblies.Contains(t.Assembly.GetName().Name)).ToList();

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
        if (type == typeof(double)) return SupplierOrFallback(knob, 0d).ResetAndGet();
        if (type == typeof(float)) return (float) SupplierOrFallback(knob, 0d).ResetAndGet();
        if (type == typeof(int)) return (int) SupplierOrFallback(knob, 0d).ResetAndGet();
        if (type == typeof(bool)) return SupplierOrFallback(knob, 0d).ResetAndGet() > 0f;
        if (type == typeof(TerrainDef)) return SupplierOrFallback<TerrainDef>(knob, null).ResetAndGet();
        if (type == typeof(BiomeDef)) return SupplierOrFallback<BiomeDef>(knob, null).ResetAndGet();
        if (type == typeof(RoofDef)) return SupplierOrFallback<RoofDef>(knob, null).ResetAndGet();
        if (type == typeof(ModuleBase)) return SupplierOrFallback(knob, GridFunction.Zero).ResetAndGet().AsModule();
        return null;
    }

    private string ShortTypeName(string fullName)
    {
        if (fullName == null) return "None";
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

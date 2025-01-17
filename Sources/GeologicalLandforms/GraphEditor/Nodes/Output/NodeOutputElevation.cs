using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "Output/Elevation", 400)]
public class NodeOutputElevation : NodeOutputBase
{
    public const string ID = "outputElevation";
    public override string GetID => ID;

    public override string Title => "Elevation Output";

    public override ValueConnectionKnob InputKnobRef => InputKnob;
    public override ValueConnectionKnob OutputKnobRef => OutputKnob;

    [ValueConnectionKnob("Elevation", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;

    [ValueConnectionKnob("ElevationOutput", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.OutputElevation;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.OutputElevation = this;
    }

    protected override void OnDelete()
    {
        if (Landform.OutputElevation == this) Landform.OutputElevation = null;
    }

    public IGridFunction<double> Get()
    {
        return InputKnob.GetValue<ISupplier<IGridFunction<double>>>()?.ResetAndGet();
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }

    public class ElevationPreviewModel : NodeGridPreview.IPreviewModel
    {
        public Color GetColorFor(float val, int x, int y)
        {
            if (val >= 0.7f) return Color.white;
            if (val >= 0.61f) return new Color(0.4f, 0.4f, 0.4f);
            if (val >= 0.55f) return new Color(0.2f, 0.2f, 0.2f);
            return Color.black;
        }
    }
}

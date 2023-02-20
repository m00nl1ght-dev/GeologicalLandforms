using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class RoofFunctionConnection : ValueConnectionType
{
    public const string Id = "RoofFunc";

    public override string Identifier => Id;
    public override Color Color => new(1.2F, 0.7F, 0.44F);
    public override Type Type => typeof(ISupplier<RoofData>);
}

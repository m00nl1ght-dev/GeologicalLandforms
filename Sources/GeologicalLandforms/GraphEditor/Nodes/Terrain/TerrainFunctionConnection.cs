using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class TerrainFunctionConnection : ValueConnectionType
{
    public const string Id = "TerrainFunc";

    public override string Identifier => Id;
    public override Color Color => new(1.5f, 0f, 0f);
    public override Type Type => typeof(ISupplier<TerrainData>);
}

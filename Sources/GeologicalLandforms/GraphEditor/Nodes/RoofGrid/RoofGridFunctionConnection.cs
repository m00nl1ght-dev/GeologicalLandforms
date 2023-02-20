using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class RoofGridFunctionConnection : ValueConnectionType
{
    public const string Id = "RoofGridFunc";

    public override string Identifier => Id;
    public override Color Color => new(0.9F, 0.45F, 0.21F);
    public override Type Type => typeof(ISupplier<IGridFunction<RoofData>>);
}

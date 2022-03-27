using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class TerrainGridFunctionConnection : ValueConnectionType
{
    public const string Id = "TerrainGridFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(1.25f, 0f, 2.5f);
    public override Type Type => typeof(ISupplier<IGridFunction<TerrainData>>);
}
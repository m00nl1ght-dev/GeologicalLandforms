using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class BiomeGridFunctionConnection : ValueConnectionType
{
    public const string Id = "BiomeGridFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(2f, 1f, 0f);
    public override Type Type => typeof(ISupplier<IGridFunction<BiomeData>>);
}
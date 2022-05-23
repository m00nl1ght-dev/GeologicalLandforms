using System;
using NodeEditorFramework;
using TerrainGraph;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class BiomeFunctionConnection : ValueConnectionType
{
    public const string Id = "BiomeFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(1.5f, 1.5f, 0f);
    public override Type Type => typeof(ISupplier<BiomeData>);
}
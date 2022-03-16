using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

public class ValueFunctionConnection : ValueConnectionType
{
    public const string Id = "ValueFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(0f, 1f, 1.3f);
    public override Type Type => typeof(ISupplier<double>);
}
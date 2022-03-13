using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

public class ValueFunctionConnection : ValueConnectionType
{
    public const string Id = "ValueFunc";
    
    public override string Identifier => Id;
    public override Color Color => Color.cyan;
    public override Type Type => typeof(ISupplier<double>);
}
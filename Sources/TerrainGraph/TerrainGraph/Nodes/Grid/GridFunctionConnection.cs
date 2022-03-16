using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

public class GridFunctionConnection : ValueConnectionType
{
    public const string Id = "GridFunc";
    
    public override string Identifier => Id;
    public override Color Color => new Color(0.51f, 1.25f, 0.51f);
    public override Type Type => typeof(ISupplier<IGridFunction<double>>);
}
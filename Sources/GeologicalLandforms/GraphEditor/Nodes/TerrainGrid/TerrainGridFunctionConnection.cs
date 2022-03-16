using System;
using NodeEditorFramework;
using UnityEngine;
using Verse;

namespace TerrainGraph;

public class TerrainGridFunctionConnection : ValueConnectionType
{
    public const string Id = "TerrainGridFunc";
    
    public override string Identifier => Id;
    public override Color Color => new(1.25f, 0f, 2.5f);
    public override Type Type => typeof(ISupplier<IGridFunction<TerrainDef>>);
}
using System;
using NodeEditorFramework;
using UnityEngine;
using Verse.Noise;

namespace GeologicalLandforms.TerrainGraph;

public class ConnectionFloat : ValueConnectionType
{
    public const string Id = "FloatFunc";
    
    public override string Identifier => Id;
    public override Color Color => Color.cyan;
    public override Type Type => typeof(Func<float>);
}
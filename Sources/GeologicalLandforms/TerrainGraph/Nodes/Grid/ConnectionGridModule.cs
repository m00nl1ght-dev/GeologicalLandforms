using System;
using NodeEditorFramework;
using UnityEngine;
using Verse.Noise;

namespace GeologicalLandforms.TerrainGraph;

public class ConnectionGridModule : ValueConnectionType
{
    public const string Id = "GridFunc";
    
    public override string Identifier => Id;
    public override Color Color => new Color(0.51f, 1f, 0.51f);
    public override Type Type => typeof(Func<ModuleBase>);
}
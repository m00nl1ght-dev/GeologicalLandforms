using RimWorld;
using UnityEngine;

namespace GeologicalLandforms.GraphEditor;

public class BiomeFunctionConnection : DefFunctionConnection<BiomeDef>
{
    public static readonly BiomeFunctionConnection Instance = new();

    public const string Id = "BiomeFunc";

    public override string Identifier => Id;
    public override Color Color => new(1.5f, 1.5f, 0f);
}

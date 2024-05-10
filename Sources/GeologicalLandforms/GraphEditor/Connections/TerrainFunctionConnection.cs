using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class TerrainFunctionConnection : DefFunctionConnection<TerrainDef>
{
    public static readonly TerrainFunctionConnection Instance = new();

    public const string Id = "TerrainFunc";

    public override string Identifier => Id;
    public override Color Color => new(1.5f, 0f, 0f);
}

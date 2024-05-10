using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class RoofFunctionConnection : DefFunctionConnection<RoofDef>
{
    public static readonly RoofFunctionConnection Instance = new();

    public const string Id = "RoofFunc";

    public override string Identifier => Id;
    public override Color Color => new(1.2F, 0.7F, 0.44F);
}

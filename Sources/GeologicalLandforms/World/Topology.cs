using static GeologicalLandforms.Topology;

namespace GeologicalLandforms;

public enum Topology : byte
{
    Any,
    Inland,
    CoastOneSide,
    CoastTwoSides,
    CoastLandbridge,
    CoastThreeSides,
    CoastAllSides,
    CliffOneSide,
    CliffTwoSides,
    CliffValley,
    CliffThreeSides,
    CliffAllSides,
    CliffAndCoast,
    CaveEntrance,
    CaveTunnel,
    Ocean
}

public static class TopologyExtensions
{
    public static bool IsCoast(this Topology topology, bool includeSpecial = false)
    {
        return topology is CoastOneSide or CoastTwoSides or CoastThreeSides or CliffAndCoast
               || (includeSpecial && topology is CoastLandbridge or CoastAllSides);
    }

    public static bool IsCliff(this Topology topology, bool includeSpecial = false)
    {
        return topology is CliffOneSide or CliffTwoSides or CliffThreeSides or CliffAllSides
               || (includeSpecial && topology is CliffValley or CliffAndCoast);
    }

    public static bool IsCommon(this Topology topology)
    {
        return topology is CliffOneSide or CliffTwoSides or CoastOneSide or CoastTwoSides;
    }

    public static bool IsCompatible(this Topology req, Topology tile, bool lenient)
    {
        if (req == Any || tile == req) return true;
        if (!lenient) return false;
        if (req.IsCoast() && tile.IsCoast(true)) return true;
        if (req.IsCliff() && tile.IsCliff(true)) return true;
        if (req == Inland && tile.IsCliff(true)) return true;
        return false;
    }
}

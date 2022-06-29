using static GeologicalLandforms.Topology;

namespace GeologicalLandforms;

public enum Topology
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
}
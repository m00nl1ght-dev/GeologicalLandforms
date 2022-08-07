using System;
using GeologicalLandforms.GraphEditor;
using Verse;

namespace GeologicalLandforms;

public static class EventHooks
{
    public static event Action OnMainMenuOnce;
    public static void RunOnMainMenuOnce()
    {
        OnMainMenuOnce?.Invoke();
        OnMainMenuOnce = null;
    }

    public static event Action<Listing_Standard> OnTerrainTab;
    public static void RunOnTerrainTab(Listing_Standard listing)
    {
        OnTerrainTab?.Invoke(listing);
    }

    public static Func<Map, bool> CellFinderOptimizationFilter { get; private set; } = _ => true;
    public static void PutCellFinderOptimizationFilter(Func<Map, bool> filter)
    {
        CellFinderOptimizationFilter = filter ?? (_ => true);
    }
    
    public static Func<int> LandformGridSizeFunction { get; private set; } = () => Landform.DefaultGridFullSize;
    public static void PutLandformGridSizeFunction(Func<int> function)
    {
        LandformGridSizeFunction = function ?? (() => Landform.DefaultGridFullSize);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using LunarFramework.GUI;
using LunarFramework.Utility;
using NodeEditorFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

[Serializable]
[Node(false, "World/Map Incidents", 1010)]
public class NodeUIMapIncidents : NodeUIBase
{
    public const string ID = "mapIncidents";
    public override string GetID => ID;

    public override string Title => "Events and Incidents";
    public override Vector2 DefaultSize => new(400, _lastLayoutMaxY);

    public List<Entry> IncidentEntries = [];
    public List<Entry> GameConditionEntries = [];
    public List<Entry> RaidStrategyEntries = [];
    public List<Entry> ArrivalModeEntries = [];

    private float _lastLayoutMaxY = 200f;

    protected override void DoWindowContents(LayoutRect layout)
    {
        DoListing(layout, IncidentEntries);

        if (LunarGUI.Button(layout, "Add incident entry"))
        {
            var options = typeof(IncidentWorker).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !IncidentEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => IncidentEntries.Add(new Entry(e, 1f)))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        layout.Abs(20f);

        DoListing(layout, GameConditionEntries);

        if (LunarGUI.Button(layout, "Add game condition entry"))
        {
            var options = typeof(GameCondition).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !GameConditionEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => GameConditionEntries.Add(new Entry(e, 1f)))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        layout.Abs(20f);

        DoListing(layout, RaidStrategyEntries);

        if (LunarGUI.Button(layout, "Add raid strategy entry"))
        {
            var options = typeof(RaidStrategyWorker).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !RaidStrategyEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => RaidStrategyEntries.Add(new Entry(e, 1f)))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        layout.Abs(20f);

        DoListing(layout, ArrivalModeEntries);

        if (LunarGUI.Button(layout, "Add arrival mode entry"))
        {
            var options = typeof(PawnsArrivalModeWorker).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !ArrivalModeEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => ArrivalModeEntries.Add(new Entry(e, 1f)))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        _lastLayoutMaxY = layout.OccupiedSpace + 50f;
    }

    private static void DoListing(LayoutRect layout, List<Entry> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];

            layout.PushChanged();

            layout.BeginAbs(28f, new LayoutParams { Horizontal = true, Spacing = 10f });

            LunarGUI.Label(layout.Abs(30f).Moved(0f, -4f), entry.Value.ToString("F2"));
            LunarGUI.Slider(layout.Abs(80f), ref entry.Value, 0f, 1f);
            LunarGUI.Label(layout.Abs(215f).Moved(0f, -4f), ShortName(entry.WorkerName));

            var removed = Widgets.ButtonImage(layout.Abs(14f).TopPart(0.5f).Moved(0f, -1f), UserInterfaceUtils.IconDelete);

            layout.End();

            if (removed)
            {
                list.RemoveAt(i);
                i--;
            }
            else if (layout.PopChanged())
            {
                list[i] = entry;
            }
        }
    }

    private static int QueueIntervalsPassed => Current.Game.tickManager.TicksSinceSettle / 1000;

    public bool CanHaveIncidentNow(IncidentWorker worker)
    {
        if (!CanUseNow(worker.GetType(), worker.def, IncidentEntries)) return false;

        var gameCondition = worker.def.gameCondition;
        if (gameCondition != null)
        {
            if (!CanUseNow(gameCondition.conditionClass, gameCondition, GameConditionEntries)) return false;
        }

        return true;
    }

    public bool CanUseRaidStrategyNow(RaidStrategyWorker worker)
    {
        return CanUseNow(worker.GetType(), worker.def, RaidStrategyEntries);
    }

    public bool CanUseArrivalModeNow(PawnsArrivalModeWorker worker)
    {
        return CanUseNow(worker.GetType(), worker.def, ArrivalModeEntries);
    }

    private bool CanUseNow(Type type, Def def, List<Entry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.WorkerType == type)
            {
                if (entry.Value <= 0f) return false;
                if (entry.Value >= 1f) return true;
                return RandAsync.Chance(entry.Value, def.shortHash ^ QueueIntervalsPassed);
            }
        }

        return true;
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.MapIncidents;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.MapIncidents = this;

        FetchWorkerTypes(IncidentEntries);
        FetchWorkerTypes(GameConditionEntries);
        FetchWorkerTypes(RaidStrategyEntries);
        FetchWorkerTypes(ArrivalModeEntries);
    }

    private void FetchWorkerTypes(List<Entry> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            entry.FetchWorkerType();
            list[i] = entry;
        }
    }

    protected override void OnDelete()
    {
        if (Landform.MapIncidents == this) Landform.MapIncidents = null;
    }

    private static string ShortName(string str)
    {
        if (str.StartsWith("IncidentWorker_")) return str.Substring(15);
        if (str.StartsWith("GameCondition_")) return str.Substring(14);
        if (str.StartsWith("RaidStrategyWorker_")) return str.Substring(19);
        if (str.StartsWith("PawnsArrivalModeWorker_")) return str.Substring(23);
        return str;
    }

    [Serializable]
    public struct Entry
    {
        public string WorkerName;
        public float Value;

        [XmlIgnore]
        public Type WorkerType;

        public Entry(Type workerType, float value)
        {
            WorkerType = workerType;
            WorkerName = workerType.Name;
            Value = value;
        }

        internal void FetchWorkerType()
        {
            WorkerType ??= GenTypes.GetTypeInAnyAssembly(WorkerName);
        }
    }
}

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

[HotSwappable]
[Serializable]
[Node(false, "World/Map Incidents", 1010)]
public class NodeUIMapIncidents : NodeUIBase
{
    public const string ID = "mapIncidents";
    public override string GetID => ID;

    public override string Title => "Events and Incidents";
    public override Vector2 DefaultSize => new(400, _lastLayoutMaxY);

    public List<Entry> IncidentEntries = new();
    public List<Entry> RaidStrategyEntries = new();

    private float _lastLayoutMaxY = 200f;

    protected override void DoWindowContents(LayoutRect layout)
    {
        DoListing(layout, IncidentEntries);

        if (LunarGUI.Button(layout, "Add incident entry"))
        {
            var options = typeof(IncidentWorker).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !IncidentEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => AddIncidentEntry(e))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        layout.Abs(20f);

        DoListing(layout, RaidStrategyEntries);

        if (LunarGUI.Button(layout, "Add raid strategy entry"))
        {
            var options = typeof(RaidStrategyWorker).AllSubclassesNonAbstract().OrderBy(e => e.Name)
                .Where(e => !RaidStrategyEntries.Any(o => o.WorkerType == e))
                .Select(e => new FloatMenuOption(ShortName(e.Name), () => AddRaidStrategyEntry(e))).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        _lastLayoutMaxY = layout.OccupiedSpace + 50f;
    }

    private void AddIncidentEntry(Type workerType)
    {
        IncidentEntries.Add(new Entry(workerType, 1f));
    }

    private void AddRaidStrategyEntry(Type workerType)
    {
        RaidStrategyEntries.Add(new Entry(workerType, 1f));
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

            var removed = Widgets.ButtonImage(layout.Abs(14f).TopPart(0.5f).Moved(0f, -1f), TexButton.DeleteX);

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
        var workerType = worker.GetType();

        foreach (var entry in IncidentEntries)
        {
            if (entry.WorkerType == workerType)
            {
                if (entry.Value <= 0f) return false;
                if (entry.Value >= 1f) return true;
                return Rand.ChanceSeeded(entry.Value, worker.def.shortHash ^ QueueIntervalsPassed);
            }
        }

        return true;
    }

    public bool CanUseRaidStrategyNow(RaidStrategyWorker worker)
    {
        var workerType = worker.GetType();

        foreach (var entry in RaidStrategyEntries)
        {
            if (entry.WorkerType == workerType)
            {
                if (entry.Value <= 0f) return false;
                if (entry.Value >= 1f) return true;
                return Rand.ChanceSeeded(entry.Value, worker.def.shortHash ^ QueueIntervalsPassed);
            }
        }

        return true;
    }

    public override void OnCreate(bool fromGUI)
    {
        var existing = Landform.MapIncidents;
        if (existing != null && existing != this && canvas.nodes.Contains(existing)) existing.Delete();
        Landform.MapIncidents = this;

        foreach (var entry in IncidentEntries) entry.FetchWorkerType();
        foreach (var entry in RaidStrategyEntries) entry.FetchWorkerType();
    }

    protected override void OnDelete()
    {
        if (Landform.MapIncidents == this) Landform.MapIncidents = null;
    }

    private static string ShortName(string str)
    {
        if (str.StartsWith("IncidentWorker_")) return str.Substring(15);
        if (str.StartsWith("RaidStrategyWorker_")) return str.Substring(19);
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

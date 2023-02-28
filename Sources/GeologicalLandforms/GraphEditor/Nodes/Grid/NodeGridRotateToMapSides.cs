using System;
using System.Collections.Generic;
using System.Linq;
using GeologicalLandforms.GraphEditor;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Map Sides", 250)]
public class NodeGridRotateToMapSides : NodeBase
{
    public const string ID = "gridRotateToMapSides";
    public override string GetID => ID;

    public override Vector2 MinSize => new(100, 10);

    public override string Title => "Map Sides";

    [NonSerialized]
    public List<ValueConnectionKnob> InputKnobs = new();

    [NonSerialized]
    public List<ValueConnectionKnob> OutputKnobs = new();

    public List<MapSide> MapSides = new();

    public override void RefreshDynamicKnobs()
    {
        InputKnobs = dynamicConnectionPorts.Where(k => k.name.StartsWith("Input")).Cast<ValueConnectionKnob>().ToList();
        OutputKnobs = dynamicConnectionPorts.Where(k => k.name.StartsWith("Output")).Cast<ValueConnectionKnob>().ToList();
        UpdateThresholdArray();
    }

    private void UpdateThresholdArray()
    {
        while (MapSides.Count < InputKnobs.Count) MapSides.Add(MapSides.Count == 0 ? MapSide.Front : MapSides[MapSides.Count - 1]);
        while (MapSides.Count > 0 && MapSides.Count > InputKnobs.Count) MapSides.RemoveAt(MapSides.Count - 1);
    }

    public override void NodeGUI()
    {
        while (InputKnobs.Count < 1) CreateNewKnobPair();

        GUILayout.BeginVertical(BoxStyle);

        UpdateThresholdArray();

        for (int i = 0; i < InputKnobs.Count; i++)
        {
            GUILayout.BeginHorizontal(BoxStyle);

            if (GUILayout.Button(MapSides[i].ToString(), GUI.skin.box, DoubleBoxLayout))
            {
                var idx = i;
                Dropdown<MapSide>(s =>
                {
                    MapSides[idx] = s;
                    canvas.OnNodeChange(this);
                });
            }

            InputKnobs[i].SetPosition();
            OutputKnobs[i].SetPosition();

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    protected void CreateNewKnobPair()
    {
        CreateValueConnectionKnob(new("Input " + InputKnobs.Count, Direction.In, GridFunctionConnection.Id));
        CreateValueConnectionKnob(new("Output " + InputKnobs.Count, Direction.Out, GridFunctionConnection.Id));
        RefreshDynamicKnobs();
    }

    public override void FillNodeActionsMenu(NodeEditorInputInfo inputInfo, GenericMenu menu)
    {
        base.FillNodeActionsMenu(inputInfo, menu);
        menu.AddSeparator("");

        if (InputKnobs.Count < 20)
        {
            menu.AddItem(new GUIContent("Add branch"), false, CreateNewKnobPair);
            canvas.OnNodeChange(this);
        }

        if (InputKnobs.Count > 1)
        {
            menu.AddItem(new GUIContent("Remove branch"), false, () =>
            {
                DeleteConnectionPort(InputKnobs[InputKnobs.Count - 1]);
                DeleteConnectionPort(OutputKnobs[InputKnobs.Count - 1]);
                RefreshDynamicKnobs();
                canvas.OnNodeChange(this);
            });
        }
    }

    public override bool Calculate()
    {
        for (int i = 0; i < InputKnobs.Count; i++)
        {
            OutputKnobs[i].SetValue<ISupplier<IGridFunction<double>>>(new NodeGridRotate.Output(
                SupplierOrGridFixed(InputKnobs[i], GridFunction.Zero),
                Supplier.Of(MapSideToAngle(MapSides[i]) + Landform.GeneratingTile.TopologyDirection.AsAngle),
                GridSize / 2, GridSize / 2
            ));
        }

        return true;
    }

    public static double MapSideToAngle(MapSide mapSide)
    {
        return mapSide switch
        {
            MapSide.Front => 90,
            MapSide.Back => -90,
            MapSide.Left => 0,
            MapSide.Right => 180,
            _ => throw new ArgumentOutOfRangeException(nameof(mapSide), mapSide, null)
        };
    }

    public static double MapSideToWorldAngle(MapSide mapSide)
    {
        return MapSideToAngle(mapSide) - 90f;
    }

    public enum MapSide
    {
        Front,
        Back,
        Left,
        Right
    }
}

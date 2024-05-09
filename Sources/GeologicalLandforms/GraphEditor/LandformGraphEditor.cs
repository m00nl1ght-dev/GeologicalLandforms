using System.Collections.Generic;
using System.Linq;
using MapPreview;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class LandformGraphEditor : Window
{
    private NodeEditorUserCache _canvasCache;
    private LandformGraphInterface _editorInterface;

    public Landform Landform => (Landform) _canvasCache?.nodeCanvas;
    public bool HasLoadedLandform => Landform != null && Landform.Id != null;

    public static bool IsEditorOpen { get; private set; }

    public static LandformGraphEditor ActiveEditor => Find.WindowStack.WindowOfType<LandformGraphEditor>();
    public NodeEditorState EditorState => _canvasCache?.editorState;

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    public static void InitialSetup()
    {
        NodeBase.ActiveDropdownHandler = (values, action) =>
        {
            var options = values.Select((e, i) => new FloatMenuOption(e, () => { action(i); })).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        };

        NodeBase.ActiveTooltipHandler = (rect, textFunc, tdelay) =>
        {
            TooltipHandler.TipRegion(rect, new TipSignal(textFunc, textFunc.GetHashCode()) { delay = tdelay });
        };

        NodeGridPreview.RegisterPreviewModel(new NodeOutputElevation.ElevationPreviewModel(), "Elevation");

        NodePathTrace.OnError = exc => GeologicalLandformsAPI.Logger.Error("Error during flow path generation", exc);
    }

    private void Init()
    {
        NodeEditor.checkInit(true);
        LandformPreviewScheduler.Instance.Init();

        if (_canvasCache == null)
        {
            _canvasCache = new NodeEditorUserCache();
            _canvasCache.NewNodeCanvas(typeof(Landform));
        }

        _editorInterface ??= new LandformGraphInterface
        {
            Editor = this,
            CanvasCache = _canvasCache
        };

        if (_canvasCache.nodeCanvas)
            _canvasCache.nodeCanvas.Validate();

        if (Find.CurrentMap != null)
        {
            Landform.GeneratingMapSize = Find.CurrentMap.Size.ToIntVec2;
        }
        else if (Find.World != null && Current.ProgramState != ProgramState.Entry)
        {
            Landform.GeneratingMapSize = Find.World.info.initialMapSize.ToIntVec2;
        }
        else if (Find.GameInitData != null)
        {
            Landform.GeneratingMapSize = new(Find.GameInitData.mapSize, Find.GameInitData.mapSize);
        }
    }

    private void CleanUp()
    {
        ResetView();

        Landform.CleanUp();
        Landform.CleanUpGUI();

        LandformManager.SaveAllEdited();
        LandformManager.RefreshLayers();

        MapPreviewAPI.NotifyWorldChanged();
        LandformPreviewScheduler.Instance.Shutdown();
    }

    public void OpenLandform(Landform landform, NodeEditorState editorState = null)
    {
        if (HasLoadedLandform)
        {
            Landform.CleanUp();
            Landform.CleanUpGUI();
        }

        if (landform != null)
        {
            _canvasCache.nodeCanvas = landform;

            if (editorState != null)
            {
                _canvasCache.editorState = editorState;
            }
            else
            {
                _canvasCache.NewEditorState();
            }

            landform.PrepareGUI();
            ResetView();

            if (WorldTileUtils.CurrentWorldTile is WorldTileInfo tileInfo && tileInfo.HasLandform(Landform))
            {
                ApplyRealTileContext(tileInfo);
            }
            else
            {
                ApplyMockTileContext();
            }
        }
        else
        {
            _canvasCache.NewNodeCanvas(typeof(Landform));
            _canvasCache.NewEditorState();
        }
    }

    public void ApplyRealTileContext(WorldTileInfo tileInfo)
    {
        var tileCopy = new WorldTileInfoPrimer(tileInfo);
        var tileSeed = SeedRerollData.GetMapSeed(Find.World, tileCopy.TileId);

        if (tileCopy.Landforms == null)
        {
            tileCopy.Landforms = [Landform];
        }
        else if (!tileCopy.Landforms.Contains(Landform))
        {
            List<Landform> landforms = [..tileCopy.Landforms, Landform];
            landforms.Sort((a, b) => a.Priority - b.Priority);
            tileCopy.Landforms = landforms;
        }

        Landform.Prepare(tileCopy, Landform.GeneratingMapSize, tileSeed);
    }

    public void ApplyMockTileContext()
    {
        var mockTile = Landform.GeneratingTile as EditorMockTileInfo ?? new EditorMockTileInfo();
        mockTile.LandformsList = [Landform];

        Landform.Prepare(mockTile, Landform.GeneratingMapSize, Landform.RandSeed);
    }

    public void Refresh()
    {
        if (Landform.GeneratingTile?.Landforms != null)
        {
            foreach (var landform in Landform.GeneratingTile.Landforms)
            {
                landform.TraverseAll();
            }
        }
    }

    public void ResetView()
    {
        _canvasCache.NewEditorState();
        _canvasCache.editorState.zoom = 1920f / UI.screenWidth;
        Landform.ResetView();
    }

    public void CloseLandform()
    {
        OpenLandform(null);
    }

    public void Duplicate()
    {
        if (HasLoadedLandform)
        {
            Landform duplicate = LandformManager.Duplicate(Landform);
            if (duplicate != null)
            {
                OpenLandform(duplicate);
            }
        }
    }

    public void Reset()
    {
        if (HasLoadedLandform)
        {
            OpenLandform(LandformManager.Reset(Landform));
        }
    }

    public void Delete()
    {
        if (HasLoadedLandform)
        {
            LandformManager.Delete(Landform);
            _canvasCache.NewNodeCanvas(typeof(Landform));
        }
    }

    public void SaveInMod()
    {
        if (HasLoadedLandform)
        {
            LandformManager.SaveInMod(Landform);
        }
    }

    public override void PreOpen()
    {
        base.PreOpen();
        IsEditorOpen = true;
        Init();
    }

    public override void PreClose()
    {
        base.PreClose();
        IsEditorOpen = false;
        CleanUp();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        NodeEditor.Update();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (NodeEditor.InitiationError)
        {
            GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
            return;
        }

        // Start Overlay GUI for popups (before any other GUI)
        OverlayGUI.StartOverlayGUI("TerrainGraphEditor");

        // Set root rect (can be any number of arbitrary groups, e.g. a nested UI, but at least one)
        GUI.BeginGroup(inRect);

        // Begin Node Editor GUI and set canvas rect
        NodeEditorGUI.StartNodeGUI(false);
        _canvasCache.editorState.canvasRect = new Rect(inRect.x, inRect.y + LandformGraphInterface.ToolbarHeight, inRect.width, inRect.height - LandformGraphInterface.ToolbarHeight);

        try
        {
            // Perform drawing with error-handling
            NodeEditor.DrawCanvas(_canvasCache.nodeCanvas, _canvasCache.editorState);
        }
        catch (UnityException e)
        {
            // On exceptions in drawing flush the canvas to avoid locking the UI
            _canvasCache.NewNodeCanvas(typeof(Landform));
            NodeEditor.ReInit(true);
            Debug.LogError("Unloaded Canvas due to exception in Draw!\n" + e);
        }

        // Draw Interface
        GUILayout.BeginArea(inRect);
        _editorInterface.DrawToolbarGUI();
        GUILayout.EndArea();
        _editorInterface.DrawModalPanel();

        // End Node Editor GUI
        NodeEditorGUI.EndNodeGUI();

        // End root rect
        GUI.EndGroup();

        // End Overlay GUI and draw popups
        OverlayGUI.EndOverlayGUI();
    }
}

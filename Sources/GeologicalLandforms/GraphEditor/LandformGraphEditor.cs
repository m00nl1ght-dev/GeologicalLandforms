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

    public EditorMockTileInfo EditorTileInfo { get; private set; }

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
    }

    private void Init()
    {
        NodeEditor.checkInit(true);
        LandformPreviewScheduler.Instance.Init();
        AssureSetup();
        if (_canvasCache.nodeCanvas)
            _canvasCache.nodeCanvas.Validate();
    }

    public override void Close(bool doCloseSound = true)
    {
        ResetView();
        base.Close(doCloseSound);
        Landform.CleanUp();
        Landform.CleanUpGUI();
        LandformManager.SaveAllEdited();
        MapPreviewAPI.NotifyWorldChanged();
        LandformPreviewScheduler.Instance.Shutdown();
    }

    private void AssureSetup()
    {
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
    }

    public void OpenLandform(Landform landform, NodeEditorState editorState = null)
    {
        if (HasLoadedLandform)
        {
            Landform.CleanUp();
            Landform.CleanUpGUI();
            EditorTileInfo = null;
        }

        if (landform != null)
        {
            EditorTileInfo = new EditorMockTileInfo { LandformsList = new List<Landform> { landform } };
            Landform.PrepareEditor(EditorTileInfo);
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
            landform.TraverseAll();
            Landform.RefreshPreviews();
            ResetView();
        }
        else
        {
            _canvasCache.NewNodeCanvas(typeof(Landform));
            _canvasCache.NewEditorState();
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

    public override void PreOpen()
    {
        IsEditorOpen = true;
        base.PreOpen();
        Init();
    }

    public override void PreClose()
    {
        base.PreClose();
        IsEditorOpen = false;
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

        AssureSetup();

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
            Debug.LogError("Unloaded Canvas due to exception in Draw!");
            Debug.LogException(e);
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

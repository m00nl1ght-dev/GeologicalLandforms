using NodeEditorFramework;
using NodeEditorFramework.Standard;
using NodeEditorFramework.Utilities;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.TerrainGraph;

public class LandformGraphEditor : Window
{
    private NodeEditorUserCache _canvasCache;
    private LandformGraphInterface _editorInterface;
    
    public Landform Landform => (Landform) _canvasCache?.nodeCanvas;
    public bool HasLoadedLandform => Landform != null && Landform.Id != null;

    public override Vector2 InitialSize => new(Screen.width, Screen.height);

    protected override float Margin => 0f;

    private void Init()
    {
        NodeEditor.checkInit(false);
        AssureSetup();
        if (_canvasCache.nodeCanvas)
            _canvasCache.nodeCanvas.Validate();
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

    public void Open(Landform landform)
    {
        _canvasCache.nodeCanvas = landform;
        _canvasCache.NewEditorState();
        landform.ResetView();
    }
    
    public void Duplicate()
    {
        if (HasLoadedLandform)
        {
            Landform duplicate = LandformManager.Duplicate(Landform);
            if (duplicate != null)
            {
                Open(duplicate);
            }
        }
    }

    public void Reset()
    {
        if (HasLoadedLandform)
        {
            Open(LandformManager.Reset(Landform));
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

    public override void Close(bool doCloseSound = true)
    {
        LandformManager.SaveAllCustom();
        base.Close(doCloseSound);
    }

    public override void PreOpen()
    {
        base.PreOpen();
        Init();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        NodeEditor.Update();
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Initiation
        NodeEditor.checkInit(true);
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
        _canvasCache.editorState.canvasRect = new Rect (inRect.x, inRect.y + LandformGraphInterface.toolbarHeight, inRect.width, inRect.height - LandformGraphInterface.toolbarHeight);

        try
        { // Perform drawing with error-handling
            NodeEditor.DrawCanvas (_canvasCache.nodeCanvas, _canvasCache.editorState);
        }
        catch (UnityException e)
        { // On exceptions in drawing flush the canvas to avoid locking the UI
            _canvasCache.NewNodeCanvas ();
            NodeEditor.ReInit (true);
            Debug.LogError ("Unloaded Canvas due to exception in Draw!");
            Debug.LogException (e);
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
using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Preview")]
public class NodeGridPreview : NodeBase
{
    public const string ID = "gridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";
    public int PreviewSize => TerrainCanvas?.GridPreviewSize ?? 100;
    public override Vector2 DefaultSize => new(PreviewSize, PreviewSize + 20);
    
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    [NonSerialized]
    private Texture2D _previewTexture;

    [NonSerialized] 
    private GridFunction _previewFunction;

    public override void PrepareGUI()
    {
        _previewTexture = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGB24, false);
    }

    public override void CleanUpGUI()
    {
        _previewTexture = null;
        _previewFunction = null;
    }

    public override void NodeGUI()
    {
        InputKnob.SetPosition(FirstKnobPosition);
        OutputKnob.SetPosition(FirstKnobPosition);

        if (_previewTexture != null)
        {
            Rect pRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize);
            GUI.DrawTexture(pRect, _previewTexture);
            
            if (Event.current.type == EventType.Repaint)
            {
                ActiveTooltipHandler?.Invoke(pRect, () =>
                {
                    Vector2 pos = NodeEditor.ScreenToCanvasSpace(Event.current.mousePosition) - rect.min - contentOffset;
                    double previewRatio = TerrainCanvas.GridPreviewRatio;
                    
                    double x = Math.Max(0, Math.Min(PreviewSize, pos.x)) * previewRatio;
                    double y = Math.Max(0, Math.Min(PreviewSize, pos.y)) * previewRatio;

                    double value = _previewFunction?.ValueAt(x, y) ?? 0;
                    return Math.Round(value, 2) + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
                }, 0f);
            }
        }

        if (GUI.changed)
            NodeEditor.curNodeCanvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<GridFunction>>());
        return true;
    }

    public override void RefreshPreview()
    {
        var previewRatio = TerrainCanvas.GridPreviewRatio;
        _previewFunction = InputKnob.connected() ? InputKnob.GetValue<ISupplier<GridFunction>>().ResetAndGet() : GridFunction.Zero;
        
        for (int x = 0; x < TerrainCanvas.GridPreviewSize; x++)
        {
            for (int y = 0; y < TerrainCanvas.GridPreviewSize; y++)
            {
                var val = (float) _previewFunction.ValueAt(x * previewRatio, y * previewRatio);
                Color color = val switch
                {
                    < -5f => new Color(0f, 0.5f, 0f),
                    < -1f => new Color(0f, - (val + 1f) / 8f, 0.5f + (val + 1f) / 8f),
                    < 0f => new Color(0f, 0f, - val / 2f),
                    < 1f => new Color(val, val, val),
                    < 2f => new Color(1f, 1f, 2f - val),
                    < 5f => new Color(1f, 1f - (val - 2f) / 3f, 0f),
                    _ => new Color(1f, 0f, 0f)
                };
                _previewTexture.SetPixel(x, y, color);
            }
        }
        
        _previewTexture.Apply();
    }
}
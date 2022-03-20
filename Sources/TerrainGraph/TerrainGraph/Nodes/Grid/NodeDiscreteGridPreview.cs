using System;
using NodeEditorFramework;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
public abstract class NodeDiscreteGridPreview<T> : NodeBase
{
    public int PreviewSize => TerrainCanvas?.GridPreviewSize ?? 100;
    public override Vector2 DefaultSize => new(PreviewSize, PreviewSize + 20);
    public override bool AutoLayout => false;
    
    public abstract ValueConnectionKnob InputKnobRef { get; }
    public abstract ValueConnectionKnob OutputKnobRef { get; }
    
    [NonSerialized]
    private Texture2D _previewTexture;

    [NonSerialized] 
    private IGridFunction<T> _previewFunction;

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
        InputKnobRef.SetPosition(FirstKnobPosition);
        OutputKnobRef.SetPosition(FirstKnobPosition);

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
                    double y = GridSize - Math.Max(0, Math.Min(PreviewSize, pos.y)) * previewRatio;

                    T value = _previewFunction == null ? default : _previewFunction.ValueAt(x, y);
                    return MakeTooltip(value, x, y);
                }, 0f);
            }
        }
    }

    protected abstract string MakeTooltip(T value, double x, double y);

    protected abstract Color GetColor(T value);

    protected abstract IGridFunction<T> Default { get; }

    public override void RefreshPreview()
    {
        var previewRatio = TerrainCanvas.GridPreviewRatio;
        _previewFunction = InputKnobRef.connected() ? InputKnobRef.GetValue<ISupplier<IGridFunction<T>>>().ResetAndGet() : Default;
        
        for (int x = 0; x < TerrainCanvas.GridPreviewSize; x++)
        {
            for (int y = 0; y < TerrainCanvas.GridPreviewSize; y++)
            {
                Color color = GetColor(_previewFunction.ValueAt(x * previewRatio, y * previewRatio));
                _previewTexture.SetPixel(x, y, color);
            }
        }
        
        _previewTexture.Apply();
    }
}
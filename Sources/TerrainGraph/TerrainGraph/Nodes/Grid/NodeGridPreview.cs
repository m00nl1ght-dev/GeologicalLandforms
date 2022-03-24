using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Preview", 215)]
public class NodeGridPreview : NodeBase
{
    public const string ID = "gridPreview";
    public override string GetID => ID;

    public override string Title => "Preview";
    public int PreviewSize => TerrainCanvas?.GridPreviewSize ?? 100;
    public override Vector2 DefaultSize => new(PreviewSize, PreviewSize + 20);
    public override bool AutoLayout => false;
    
    [ValueConnectionKnob("Input", Direction.In, GridFunctionConnection.Id)]
    public ValueConnectionKnob InputKnob;
    
    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;

    public string PreviewModelId = "Default";
    
    [NonSerialized]
    private Texture2D _previewTexture;

    [NonSerialized] 
    private IGridFunction<double> _previewFunction;

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
                    double y = GridSize - Math.Max(0, Math.Min(PreviewSize, pos.y)) * previewRatio;

                    double value = _previewFunction?.ValueAt(x, y) ?? 0;
                    return Math.Round(value, 2) + " ( " + Math.Round(x, 0) + " | " + Math.Round(y, 0) + " )";
                }, 0f);
            }
        }
    }
    
    public override void FillNodeActionsMenu(NodeEditorInputInfo inputInfo, GenericMenu menu)
    {
        base.FillNodeActionsMenu(inputInfo, menu);
        menu.AddSeparator("");
        
        SelectionMenu(menu, PreviewModels.Keys.ToList(), SetModel, e => "Set preview model/"+e);
    }

    private void SetModel(string id)
    {
        PreviewModelId = id;
        canvas.OnNodeChange(this);
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue(InputKnob.GetValue<ISupplier<IGridFunction<double>>>());
        return true;
    }

    public override void RefreshPreview()
    {
        var previewRatio = TerrainCanvas.GridPreviewRatio;
        PreviewModels.TryGetValue(PreviewModelId, out IPreviewModel previewModel);
        previewModel ??= DefaultModel;
        
        _previewFunction = InputKnob.connected() ? InputKnob.GetValue<ISupplier<IGridFunction<double>>>().ResetAndGet() : GridFunction.Zero;
        
        for (int x = 0; x < TerrainCanvas.GridPreviewSize; x++)
        {
            for (int y = 0; y < TerrainCanvas.GridPreviewSize; y++)
            {
                var val = (float) _previewFunction.ValueAt(x * previewRatio, y * previewRatio);
                _previewTexture.SetPixel(x, y, previewModel.GetColorFor(val, x, y));
            }
        }
        
        _previewTexture.Apply();
    }

    private static readonly Dictionary<string, IPreviewModel> PreviewModels = new();
    public static readonly IPreviewModel DefaultModel = new DefaultPreviewModel();
    
    public static void RegisterPreviewModel(IPreviewModel model, string id)
    {
        PreviewModels.Add(id, model);
    }

    static NodeGridPreview()
    {
        RegisterPreviewModel(DefaultModel, "Default");
    }
    
    public interface IPreviewModel
    {
        public Color GetColorFor(float val, int x, int y);
    }

    private class DefaultPreviewModel : IPreviewModel
    {
        public Color GetColorFor(float val, int x, int y)
        {
            return val switch
            {
                < -5f => new Color(0f, 0.5f, 0f),
                < -1f => new Color(0f, - (val + 1f) / 8f, 0.5f + (val + 1f) / 8f),
                < 0f => new Color(0f, 0f, - val / 2f),
                < 1f => new Color(val, val, val),
                < 2f => new Color(1f, 1f, 2f - val),
                < 5f => new Color(1f, 1f - (val - 2f) / 3f, 0f),
                _ => new Color(1f, 0f, 0f)
            };
        }
    }
}
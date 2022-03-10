using NodeEditorFramework;

namespace GeologicalLandforms.TerrainGraph.Nodes;

public abstract class AbstractLandformGraphNode : Node
{
    public Landform Landform => (Landform) canvas;
}
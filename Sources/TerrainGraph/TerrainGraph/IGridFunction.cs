namespace TerrainGraph;

public interface IGridFunction<out T>
{
    public abstract T ValueAt(double x, double z);
}
namespace TerrainGraph;

public interface ISupplier<out T>
{
    public abstract T Get();

    public abstract void ResetState();
}
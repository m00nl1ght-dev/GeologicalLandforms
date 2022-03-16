namespace TerrainGraph;

public static class Supplier
{
    public static Const<double> Zero = Of<double>(0f);
    public static Const<double> One = Of<double>(1f);
    
    public static Const<T> Of<T>(T value) => new(value);
    
    public class Const<T> : ISupplier<T>
    {
        public readonly T Value;

        public Const(T value) => Value = value;

        public T Get() => Value;
        
        public void ResetState() {}
    }

    public static T ResetAndGet<T>(this ISupplier<T> supplier)
    {
        supplier.ResetState();
        return supplier.Get();
    }
}
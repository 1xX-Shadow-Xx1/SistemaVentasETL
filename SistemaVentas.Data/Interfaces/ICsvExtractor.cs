namespace SistemaVentas.Data.Interfaces
{
    public interface ICsvExtractor<T> : IExtractor<T> where T : class
    {
        string FilePath { get; }
    }
}

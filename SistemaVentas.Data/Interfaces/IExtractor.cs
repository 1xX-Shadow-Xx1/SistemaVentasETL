namespace SistemaVentas.Data.Interfaces
{
    public interface IExtractor<T> where T : class
    {
        Task<IEnumerable<T>> ExtractAsync();
    }
}


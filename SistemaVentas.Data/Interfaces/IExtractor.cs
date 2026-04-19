namespace SistemaVentas.Data.Interfaces
{
    public interface IExtractor<T> where T : class
    {
        string SourceType { get; }
        string EntityName { get; }
        string SourceName { get; }

        Task<IEnumerable<T>> ExtractAsync();

        Task<IEnumerable<T>> ExtractAsync(string source)
        {
            return ExtractAsync();
        }
    }
}


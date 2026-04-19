namespace SistemaVentas.Application.Interfaces
{
    public interface ICsvExtractor<T> : IExtractor<T> where T : class
    {
    }
}

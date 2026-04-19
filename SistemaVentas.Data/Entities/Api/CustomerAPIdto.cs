namespace SistemaVentas.Data.Entities.Api
{
    public class CustomerAPIDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
    }
}

namespace SistemaVentas.Domain.Entities.Api
{
    public class OrderAPIDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

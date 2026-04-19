namespace SistemaVentas.Data.Entities.Csv
{
    public class OrderCsv
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

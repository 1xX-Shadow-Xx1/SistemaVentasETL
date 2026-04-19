namespace SistemaVentas.Data.Entities.Csv
{
    public class OrderDetailCsv
    {
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

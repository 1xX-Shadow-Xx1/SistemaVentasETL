namespace SistemaVentas.Data.Entities.Csv
{
    public class VentaCsv
    {
        public int ID_Transaccion { get; set; }
        public int ID_Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal Total_Venta { get; set; }
    }
}

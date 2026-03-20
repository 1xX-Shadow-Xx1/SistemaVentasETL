namespace SistemaVentas.Data.Entities.Dwh.Facts
{
    public class FactVenta
    {
        public int ID_Venta { get; set; }
        public int ID_Transaccion { get; set; }
        public int ID_Tiempo { get; set; }
        public int ID_Producto { get; set; }
        public int ID_Cliente { get; set; }
        public int ID_Ubicacion { get; set; }


        public int Cantidad { get; set; }
        public decimal Precio_Unitario { get; set; }
        public decimal Total_Venta { get; set; }


        public string Origen_Datos { get; set; } = string.Empty;
    }
}

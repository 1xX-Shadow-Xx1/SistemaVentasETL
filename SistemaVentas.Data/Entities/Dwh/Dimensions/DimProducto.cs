namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    public class DimProducto
    {
        public int ID_Producto { get; set; }
        public string Nombre_Producto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Precio_Base { get; set; }
    }
}

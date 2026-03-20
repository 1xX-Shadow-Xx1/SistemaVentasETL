namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    public class DimCliente
    {
        public int ID_Cliente { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Tipo_Cliente { get; set; } = string.Empty;
    }
}

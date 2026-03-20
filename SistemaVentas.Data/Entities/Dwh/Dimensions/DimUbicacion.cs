namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    public class DimUbicacion
    {
        public int ID_Ubicacion { get; set; }
        public string Pais { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
    }
}


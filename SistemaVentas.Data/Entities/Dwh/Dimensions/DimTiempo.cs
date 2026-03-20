namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    public class DimTiempo
    {
        public int ID_Tiempo { get; set; } // Formato YYYYMMDD
        public DateTime Fecha { get; set; }
        public int Anio { get; set; }
        public int Trimestre { get; set; }
        public int Mes { get; set; }
        public string Nombre_Mes { get; set; } = string.Empty;
        public int Dia { get; set; }
    }
}

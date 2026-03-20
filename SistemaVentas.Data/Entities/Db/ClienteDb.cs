namespace SistemaVentas.Data.Entities.Db
{
    public class ClienteDb
    {
        public int ID_Cliente { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Tipo_Cliente { get; set; } = string.Empty;
    }
}

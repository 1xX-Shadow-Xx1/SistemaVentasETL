using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Domain.Entities.Dwh.Dimensions
{
    [Table("Dim_Tiempo")]
    public class DimTiempo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID_Tiempo { get; set; }
        public DateTime Fecha { get; set; }
        public int Anio { get; set; }
        public int Trimestre { get; set; }
        public int Mes { get; set; }
        public string Nombre_Mes { get; set; } = string.Empty;
        public int Dia { get; set; }
    }
}

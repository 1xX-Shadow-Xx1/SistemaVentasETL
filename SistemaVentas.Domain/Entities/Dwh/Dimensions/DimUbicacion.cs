using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Domain.Entities.Dwh.Dimensions
{
    [Table("Dim_Ubicacion")]
    public class DimUbicacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID_Ubicacion { get; set; }
        public string Pais { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
    }
}


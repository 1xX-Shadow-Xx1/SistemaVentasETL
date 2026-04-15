using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    [Table("Dim_Cliente")]
    public class DimCliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID_Cliente { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Tipo_Cliente { get; set; } = string.Empty;
    }
}

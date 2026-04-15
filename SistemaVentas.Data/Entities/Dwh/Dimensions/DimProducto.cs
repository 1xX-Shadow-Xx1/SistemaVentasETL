using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Data.Entities.Dwh.Dimensions
{
    [Table("Dim_Producto")]
    public class DimProducto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID_Producto { get; set; }
        public string Nombre_Producto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio_Base { get; set; }
    }
}

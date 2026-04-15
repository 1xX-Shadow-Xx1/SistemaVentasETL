using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Data.Entities.Dwh.Facts
{
    [Table("Fact_Ventas")]
    public class FactVenta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_Venta { get; set; }
        public int ID_Transaccion { get; set; }
        public int ID_Tiempo { get; set; }
        public int ID_Producto { get; set; }
        public int ID_Cliente { get; set; }
        public int ID_Ubicacion { get; set; }
        public int Cantidad { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio_Unitario { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total_Venta { get; set; }
        public string Origen_Datos { get; set; } = string.Empty;
    }
}

using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data.Entities.Dwh.Dimensions;
using SistemaVentas.Data.Entities.Dwh.Facts;

namespace SistemaVentas.Data.Persistence.Context
{
    public class VentasDwhContext : DbContext
    {
        public VentasDwhContext(DbContextOptions<VentasDwhContext> options) : base(options)
        {
        }

        public DbSet<DimCliente> DimClientes { get; set; }
        public DbSet<DimProducto> DimProductos { get; set; }
        public DbSet<DimTiempo> DimTiempos { get; set; }
        public DbSet<DimUbicacion> DimUbicaciones { get; set; }
        public DbSet<FactVenta> FactVentas { get; set; }
    }
}

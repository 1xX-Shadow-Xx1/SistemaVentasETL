using SistemaVentas.Domain.Entities.Dwh.Dimensions;
using SistemaVentas.Domain.Entities.Dwh.Facts;
using Microsoft.EntityFrameworkCore;

namespace SistemaVentas.Persistence.Repositories.Dwh.Context
{
    public class VentasDwhContext : DbContext
    {
        public VentasDwhContext(DbContextOptions<VentasDwhContext> options) : base(options) { }

        public DbSet<DimCliente> DimClientes { get; set; }
        public DbSet<DimProducto> DimProductos { get; set; }
        public DbSet<DimUbicacion> DimUbicaciones { get; set; }
        public DbSet<DimTiempo> DimTiempos { get; set; }
        public DbSet<FactVenta> FactVentas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DimCliente>().ToTable("Dim_Cliente").HasKey(e => e.ID_Cliente);
            modelBuilder.Entity<DimProducto>().ToTable("Dim_Producto").HasKey(e => e.ID_Producto);
            modelBuilder.Entity<DimUbicacion>().ToTable("Dim_Ubicacion").HasKey(e => e.ID_Ubicacion);
            modelBuilder.Entity<DimTiempo>().ToTable("Dim_Tiempo").HasKey(e => e.ID_Tiempo);
            modelBuilder.Entity<FactVenta>().ToTable("Fact_Ventas").HasKey(e => new { e.ID_Transaccion, e.Origen_Datos, e.ID_Producto });
        }
    }
}

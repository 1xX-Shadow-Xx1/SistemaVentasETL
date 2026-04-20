using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Data.Models;
using SistemaVentas.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaVentas.Persistence.Repositories.Db
{
    public class VentasDbContext : DbContext
    {
        public VentasDbContext(DbContextOptions<VentasDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Status> Statuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<City>().ToTable("City");
            modelBuilder.Entity<Country>().ToTable("Country");
            modelBuilder.Entity<Customer>().ToTable("Customer");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Status>().ToTable("Status");
            
            modelBuilder.Entity<OrderDetail>()
                .ToTable("Order_Detail")
                .HasKey(od => new { od.OrderId, od.ProductId });
        }
    }

    public class VentasDbData
    {
        public List<Category> Categories { get; set; } = new();
        public List<City> Cities { get; set; } = new();
        public List<Country> Countries { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<OrderDetail> OrderDetails { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Status> Statuses { get; set; } = new();
    }

    public class ClienteDbExtractor : IDatabaseExtractor<VentasDbData>
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public string SourceType => "Database";
        public string EntityName => nameof(VentasDbData);
        public string SourceName => "VentasDB";

        public ClienteDbExtractor(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("VentasDB") ?? "";
        }

        public async Task<IEnumerable<VentasDbData>> ExtractAsync()
        {
            var optionsBuilder = new DbContextOptionsBuilder<VentasDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);

            using var context = new VentasDbContext(optionsBuilder.Options);

            var dbData = new VentasDbData
            {
                Categories = await context.Categories.ToListAsync(),
                Cities = await context.Cities.ToListAsync(),
                Countries = await context.Countries.ToListAsync(),
                Customers = await context.Customers.ToListAsync(),
                Orders = await context.Orders.ToListAsync(),
                OrderDetails = await context.OrderDetails.ToListAsync(),
                Products = await context.Products.ToListAsync(),
                Statuses = await context.Statuses.ToListAsync()
            };

            return new List<VentasDbData> { dbData };
        }
    }
}

using crudEbancoDist8.Models;
using Microsoft.EntityFrameworkCore;

namespace crudEbancoDist.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {


        }
        public DbSet<Bebidas> Bebidas { get; set; } = null!;

        public DbSet<Categoria> Categorias { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bebidas>()
                .Property(b => b.Price)
                .HasPrecision(10, 2);
        }
    }
}

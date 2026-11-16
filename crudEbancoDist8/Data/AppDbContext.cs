using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace crudEbancoDist.Data
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
        }



        public DbSet<Bebidas> Bebidas { get; set; } = null!;

        public DbSet<Categoria> Categorias { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bebidas>()
                .Property(b => b.ImageUrl)
                .HasMaxLength(2048);

            modelBuilder.Entity<Bebidas>()
                .Property(b => b.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Bebidas>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Bebidas)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Bebidas>()
                .Property(b => b.ImagePublicId)
                .HasMaxLength(255);
        }
    }
}

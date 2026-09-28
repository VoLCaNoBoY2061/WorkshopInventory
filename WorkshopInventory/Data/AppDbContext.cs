using System.IO;
using Microsoft.EntityFrameworkCore;
using WorkshopInventory.Models;

namespace WorkshopInventory.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<MaterialStock> MaterialStocks => Set<MaterialStock>();
        public DbSet<Receipt> Receipts => Set<Receipt>();
        public DbSet<WriteOff> WriteOffs => Set<WriteOff>();
        public DbSet<Movement> Movements => Set<Movement>();
        public DbSet<MaterialRequest> Requests => Set<MaterialRequest>();

        // Файл базы данных лежит рядом с exe — сервер СУБД не нужен.
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "workshop.db");

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Material>().Property(m => m.MinQuantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<MaterialStock>().Property(s => s.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<Receipt>().Property(r => r.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<WriteOff>().Property(w => w.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<Movement>().Property(m => m.Quantity).HasColumnType("decimal(18,3)");
            modelBuilder.Entity<MaterialRequest>().Property(r => r.Quantity).HasColumnType("decimal(18,3)");

            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();

            // У одного материала не может быть двух отдельных остатков на одном и том же складе —
            // это должна быть одна строка, которую мы увеличиваем/уменьшаем.
            modelBuilder.Entity<MaterialStock>().HasIndex(s => new { s.MaterialId, s.Location }).IsUnique();
        }
    }
}

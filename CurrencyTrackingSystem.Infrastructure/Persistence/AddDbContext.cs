using CurrencyTrackingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Currency> Currencies => Set<Currency>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserFavoriteCurrency> UserFavoriteCurrencies => Set<UserFavoriteCurrency>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Только то, что нельзя выразить через атрибуты
            modelBuilder.Entity<UserFavoriteCurrency>(entity =>
            {
                entity.HasKey(uf => new { uf.UserId, uf.CurrencyId });

                entity.HasOne(uf => uf.User)
                    .WithMany(u => u.FavoriteCurrencies)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Глобальная настройка точности для decimal
            configurationBuilder.Properties<decimal>()
                .HavePrecision(18, 6);

            base.ConfigureConventions(configurationBuilder);
        }
    }
}

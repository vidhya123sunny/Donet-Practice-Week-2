using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(80).IsRequired();
            entity.HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Groceries" },
                new Category { Id = 3, Name = "Stationery" });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(120).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.RowVersion).IsConcurrencyToken();

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.ProductDetail)
                .WithOne(pd => pd.Product)
                .HasForeignKey<ProductDetail>(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new Product { Id = 1, Name = "Smartphone", Price = 25000, StockQuantity = 50, CategoryId = 1, RowVersion = "seed-smartphone" },
                new Product { Id = 2, Name = "Laptop", Price = 75000, StockQuantity = 15, CategoryId = 1, RowVersion = "seed-laptop" },
                new Product { Id = 3, Name = "Wheat Flour", Price = 800, StockQuantity = 100, CategoryId = 2, RowVersion = "seed-wheat" },
                new Product { Id = 4, Name = "Notebook Pack", Price = 450, StockQuantity = 200, CategoryId = 3, RowVersion = "seed-notebook" });
        });

        modelBuilder.Entity<ProductDetail>().HasData(
            new ProductDetail { ProductDetailId = 1, ProductId = 1, WarrantyInfo = "1 year manufacturer warranty" },
            new ProductDetail { ProductDetailId = 2, ProductId = 2, WarrantyInfo = "2 years extended warranty available" });

        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Name = "New Arrival" },
            new Tag { Id = 2, Name = "On Sale" },
            new Tag { Id = 3, Name = "Fast Moving" });

        modelBuilder.Entity<Product>()
            .HasMany(p => p.Tags)
            .WithMany(t => t.Products)
            .UsingEntity(j => j.HasData(
                new { ProductsId = 1, TagsId = 1 },
                new { ProductsId = 2, TagsId = 2 },
                new { ProductsId = 3, TagsId = 3 }));
    }
}

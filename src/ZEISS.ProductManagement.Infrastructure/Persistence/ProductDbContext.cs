using Microsoft.EntityFrameworkCore;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products
    {
        get
        {
            return Set<Product>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("ProductIds", "dbo")
            .StartsAt(100005)
            .IncrementsBy(1)
            .HasMin(100000)
            .HasMax(999999)
            .IsCyclic(false);

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}

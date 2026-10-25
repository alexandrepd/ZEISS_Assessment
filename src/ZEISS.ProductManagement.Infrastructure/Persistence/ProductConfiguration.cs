using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Products_Id", "[Id] BETWEEN 100000 AND 999999");
            table.HasCheckConstraint("CK_Products_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
            table.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0");
        });

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id)
            .HasDefaultValueSql("NEXT VALUE FOR [dbo].[ProductIds]")
            .ValueGeneratedOnAdd();

        builder.Property(product => product.Name)
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.MaxDescriptionLength);

        builder.Property(product => product.Price)
            .HasPrecision(18, 2);

        var createdAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new { Id = 100000, Name = "Optical lens", Description = "Precision optical lens", Price = 125.50m, Stock = 10, CreatedAtUtc = createdAtUtc },
            new { Id = 100001, Name = "Microscope", Description = "Laboratory microscope", Price = 1500m, Stock = 5, CreatedAtUtc = createdAtUtc },
            new { Id = 100002, Name = "Camera adapter", Description = "Microscope camera adapter", Price = 89.90m, Stock = 20, CreatedAtUtc = createdAtUtc },
            new { Id = 100003, Name = "Lens cleaning kit", Description = "Optical surface cleaning kit", Price = 19.99m, Stock = 0, CreatedAtUtc = createdAtUtc },
            new { Id = 100004, Name = "Calibration slide", Description = "Microscope calibration slide", Price = 45m, Stock = 8, CreatedAtUtc = createdAtUtc });
    }
}

using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.UnitTests;

public sealed class ProductUpdateTests
{
    [Fact]
    public void UpdateDetails_ChangesMetadataButPreservesStockAndCreationTime()
    {
        var product = new Product("Lens", "Original", 10m, 5);
        var createdAtUtc = product.CreatedAtUtc;

        product.UpdateDetails(" Updated lens ", null, 20m);

        Assert.Equal("Updated lens", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(20m, product.Price);
        Assert.Equal(5, product.Stock);
        Assert.Equal(createdAtUtc, product.CreatedAtUtc);
        Assert.NotNull(product.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, product.UpdatedAtUtc.Value.Kind);
    }

    [Theory]
    [InlineData("", 20)]
    [InlineData("Updated lens", 0)]
    public void UpdateDetails_WithInvalidValues_DoesNotPartiallyChangeProduct(string name, int price)
    {
        var product = new Product("Lens", "Original", 10m, 5);

        Assert.ThrowsAny<ArgumentException>(() => product.UpdateDetails(name, null, price));

        Assert.Equal("Lens", product.Name);
        Assert.Equal("Original", product.Description);
        Assert.Equal(10m, product.Price);
        Assert.Equal(5, product.Stock);
        Assert.Null(product.UpdatedAtUtc);
    }
}

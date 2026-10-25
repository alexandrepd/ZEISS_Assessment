using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesProduct()
    {
        var product = new Product("Lens", "Optical lens", 12.50m, 5);

        Assert.Equal("Lens", product.Name);
        Assert.Equal("Optical lens", product.Description);
        Assert.Equal(12.50m, product.Price);
        Assert.Equal(5, product.Stock);
        Assert.Equal(0, product.Id);
        Assert.Null(product.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, product.CreatedAtUtc.Kind);
        Assert.NotEqual(default, product.CreatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(
            () => new Product(name, null, 1m, 0));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_TrimsNameBeforeCheckingLength()
    {
        var name = new string('a', 200);

        var product = new Product($"  {name}  ", null, 1m, 0);

        Assert.Equal(name, product.Name);
    }

    [Fact]
    public void Constructor_WithNameAboveLimit_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Product(new string('a', 201), null, 1m, 0));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" Description ")]
    public void Constructor_WithOptionalDescription_PreservesValue(string? description)
    {
        var product = new Product("Lens", description, 1m, 0);

        Assert.Equal(description, product.Description);
    }

    [Fact]
    public void Constructor_WithDescriptionAtLimit_AcceptsValue()
    {
        var description = new string('a', 2000);

        var product = new Product("Lens", description, 1m, 0);

        Assert.Equal(description, product.Description);
    }

    [Fact]
    public void Constructor_WithDescriptionAboveLimit_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Product("Lens", new string('a', 2001), 1m, 0));

        Assert.Equal("description", exception.ParamName);
    }

    public static TheoryData<decimal> ValidPrices
    {
        get
        {
            return new TheoryData<decimal>
            {
                0.01m,
                1.2300m,
                9_999_999_999_999_999.99m
            };
        }
    }

    public static TheoryData<decimal> InvalidPrices
    {
        get
        {
            return new TheoryData<decimal>
            {
                0m,
                -0.01m,
                1.001m,
                10_000_000_000_000_000m
            };
        }
    }

    [Theory]
    [MemberData(nameof(ValidPrices))]
    public void Constructor_WithValidPrice_PreservesValue(decimal price)
    {
        var product = new Product("Lens", null, price, 0);

        Assert.Equal(price, product.Price);
    }

    [Theory]
    [MemberData(nameof(InvalidPrices))]
    public void Constructor_WithInvalidPrice_Throws(decimal price)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Product("Lens", null, price, 0));

        Assert.Equal("price", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void Constructor_WithValidStock_PreservesValue(int stock)
    {
        var product = new Product("Lens", null, 1m, stock);

        Assert.Equal(stock, product.Stock);
    }

    [Theory]
    [InlineData(-1)]
    public void Constructor_WithNegativeStock_Throws(int stock)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Product("Lens", null, 1m, stock));

        Assert.Equal("stock", exception.ParamName);
    }
}

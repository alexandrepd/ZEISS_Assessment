using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ZEISS.ProductManagement.Application.Products;

namespace ZEISS.ProductManagement.UnitTests;

public sealed class ProductRequestTests
{
    [Fact]
    public void EmptyRequests_RequireNamePriceAndInitialStock()
    {
        Assert.Equal(["Name", "Price", "Stock"], InvalidMembers(new CreateProductRequest()));
        Assert.Equal(["Name", "Price"], InvalidMembers(new UpdateProductRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Requests_RejectMissingNames(string? name)
    {
        AssertInvalidInBoth(name, null, 1m, "Name");
    }

    [Fact]
    public void Requests_RejectNamesAboveLimit()
    {
        AssertInvalidInBoth(new string('a', 201), null, 1m, "Name");
    }

    [Fact]
    public void Requests_TrimNamesBeforeValidatingLength()
    {
        var name = new string('a', 200);
        var create = new CreateProductRequest { Name = $" {name} ", Price = 1m, Stock = 0 };
        var update = new UpdateProductRequest { Name = $" {name} ", Price = 1m };

        Assert.Equal(name, create.Name);
        Assert.Equal(name, update.Name);
        Assert.Empty(InvalidMembers(create));
        Assert.Empty(InvalidMembers(update));
    }

    [Fact]
    public void Requests_RejectDescriptionsAboveLimit()
    {
        AssertInvalidInBoth("Lens", new string('a', 2001), 1m, "Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" Optical lens ")]
    public void Requests_PreserveOptionalDescription(string? description)
    {
        var create = new CreateProductRequest { Name = "Lens", Description = description, Price = 1m, Stock = 0 };
        var update = new UpdateProductRequest { Name = "Lens", Description = description, Price = 1m };

        Assert.Equal(description, create.Description);
        Assert.Equal(description, update.Description);
        Assert.Empty(InvalidMembers(create));
        Assert.Empty(InvalidMembers(update));
    }

    [Fact]
    public void Requests_AcceptDescriptionAtLimit()
    {
        var description = new string('a', 2000);

        Assert.Empty(InvalidMembers(new CreateProductRequest
        {
            Name = "Lens", Description = description, Price = 1m, Stock = 0
        }));
        Assert.Empty(InvalidMembers(new UpdateProductRequest
        {
            Name = "Lens", Description = description, Price = 1m
        }));
    }

    [Theory]
    [MemberData(nameof(InvalidPrices))]
    public void Requests_RejectInvalidPrices(decimal price)
    {
        AssertInvalidInBoth("Lens", null, price, "Price");
    }

    [Theory]
    [MemberData(nameof(ValidPrices))]
    public void Requests_AcceptValidPrices(decimal price)
    {
        Assert.Empty(InvalidMembers(new CreateProductRequest { Name = "Lens", Price = price, Stock = 0 }));
        Assert.Empty(InvalidMembers(new UpdateProductRequest { Name = "Lens", Price = price }));
    }

    [Theory]
    [InlineData(-1)]
    public void CreateRequest_RejectsNegativeStock(int stock)
    {
        Assert.Equal(["Stock"], InvalidMembers(new CreateProductRequest
        {
            Name = "Lens", Price = 1m, Stock = stock
        }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void CreateRequest_AcceptsNonNegativeStock(int stock)
    {
        Assert.Empty(InvalidMembers(new CreateProductRequest { Name = "Lens", Price = 1m, Stock = stock }));
    }

    [Fact]
    public void Deserialization_DistinguishesMissingStockFromZero()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var missing = JsonSerializer.Deserialize<CreateProductRequest>(
            """{"name":"Lens","price":1}""", options);
        var zero = JsonSerializer.Deserialize<CreateProductRequest>(
            """{"name":"Lens","price":1,"stock":0}""", options);

        Assert.NotNull(missing);
        Assert.Equal(["Stock"], InvalidMembers(missing));
        Assert.NotNull(zero);
        Assert.Empty(InvalidMembers(zero));
    }

    [Fact]
    public void Response_SerializesStockEvenWhenZero()
    {
        var response = new ProductResponse(100005, "Lens", null, 1m, 0, DateTime.UtcNow, null);
        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(0, json.GetProperty("stock").GetInt32());
    }

    public static TheoryData<decimal> ValidPrices => new()
    {
        0.01m,
        1.2300m,
        9_999_999_999_999_999.99m
    };

    public static TheoryData<decimal> InvalidPrices => new()
    {
        0m,
        -0.01m,
        1.001m,
        10_000_000_000_000_000m
    };

    private static void AssertInvalidInBoth(string? name, string? description, decimal price, string member)
    {
        Assert.Equal([member], InvalidMembers(new CreateProductRequest
        {
            Name = name, Description = description, Price = price, Stock = 0
        }));
        Assert.Equal([member], InvalidMembers(new UpdateProductRequest
        {
            Name = name, Description = description, Price = price
        }));
    }

    private static string[] InvalidMembers(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).Order().ToArray();
    }
}

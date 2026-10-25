using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using ZEISS.ProductManagement.Infrastructure.Persistence;

namespace ZEISS.ProductManagement.IntegrationTests;

public sealed class GetProductTests
{
    [Fact]
    public async Task GetById_ReturnsProductWithStockOrNotFoundProblem()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "GetProductTests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ProductDbContext(options);
        await context.Database.MigrateAsync();

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Products"] = connectionString
                })));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/products/100000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100000, product.GetProperty("id").GetInt32());
        Assert.Equal("Optical lens", product.GetProperty("name").GetString());
        Assert.Equal("Precision optical lens", product.GetProperty("description").GetString());
        Assert.Equal(125.50m, product.GetProperty("price").GetDecimal());
        Assert.Equal(10, product.GetProperty("stock").GetInt32());
        Assert.EndsWith("Z", product.GetProperty("createdAtUtc").GetString());
        Assert.Equal(JsonValueKind.Null, product.GetProperty("updatedAtUtc").ValueKind);

        var zeroStockProduct = await client.GetFromJsonAsync<JsonElement>("/api/products/100003");
        Assert.Equal(0, zeroStockProduct.GetProperty("stock").GetInt32());

        using var missingResponse = await client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
        var problem = await missingResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal("Product not found", problem.GetProperty("title").GetString());
        Assert.Equal("The requested product does not exist.", problem.GetProperty("detail").GetString());
    }
}

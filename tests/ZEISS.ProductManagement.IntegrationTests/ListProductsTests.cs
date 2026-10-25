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

public sealed class ListProductsTests
{
    [Fact]
    public async Task List_WithoutDatabaseConfiguration_ReturnsSafeProblemInsteadOfEmptyList()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Products"] = null
                    }));
            });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.DoesNotContain("ConnectionStrings", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public async Task List_ReturnsSeedProductsWithStockAndEmptyArrayWhenNoProductsExist()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "ListProductsTests"
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

        using var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { 100000, 100001, 100002, 100003, 100004 },
            products.EnumerateArray().Select(product => product.GetProperty("id").GetInt32()).ToArray());
        Assert.Equal(new[] { 10, 5, 20, 0, 8 },
            products.EnumerateArray().Select(product => product.GetProperty("stock").GetInt32()).ToArray());
        var first = products[0];
        Assert.Equal("Optical lens", first.GetProperty("name").GetString());
        Assert.Equal("Precision optical lens", first.GetProperty("description").GetString());
        Assert.Equal(125.50m, first.GetProperty("price").GetDecimal());
        Assert.EndsWith("Z", first.GetProperty("createdAtUtc").GetString());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("updatedAtUtc").ValueKind);

        await context.Products.ExecuteDeleteAsync();

        using var emptyResponse = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        Assert.Equal("[]", await emptyResponse.Content.ReadAsStringAsync());
    }
}

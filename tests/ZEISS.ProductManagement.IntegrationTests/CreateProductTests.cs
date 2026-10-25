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

public sealed class CreateProductTests
{
    [Fact]
    public async Task Create_PersistsProductRejectsInvalidInputAndGeneratesUniqueIdsAcrossInstances()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "CreateProductTests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ProductDbContext(options);
        await context.Database.MigrateAsync();

        await using var firstFactory = CreateFactory(connectionString);
        await using var secondFactory = CreateFactory(connectionString);
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();

        using var response = await firstClient.PostAsJsonAsync("/api/products", new
        {
            id = 999999, name = "  New lens  ", description = "Test lens", price = 12.50m, stock = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/products/100005", response.Headers.Location.ToString());
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100005, created.GetProperty("id").GetInt32());
        Assert.Equal("New lens", created.GetProperty("name").GetString());
        Assert.Equal("Test lens", created.GetProperty("description").GetString());
        Assert.Equal(12.50m, created.GetProperty("price").GetDecimal());
        Assert.Equal(0, created.GetProperty("stock").GetInt32());
        Assert.EndsWith("Z", created.GetProperty("createdAtUtc").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("updatedAtUtc").ValueKind);

        using var readResponse = await firstClient.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var saved = await readResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.ToString(), saved.ToString());

        using var invalidResponse = await firstClient.PostAsJsonAsync("/api/products",
            new { name = "Invalid", price = 1m, stock = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal(6, await context.Products.CountAsync());

        async Task<int> CreateConcurrently(int index)
        {
            var client = index % 2 == 0 ? firstClient : secondClient;
            using var result = await client.PostAsJsonAsync("/api/products",
                new { name = $"Concurrent lens {index}", price = 1m, stock = index });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var body = await result.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(index, body.GetProperty("stock").GetInt32());
            return body.GetProperty("id").GetInt32();
        }

        var ids = await Task.WhenAll(Enumerable.Range(1, 8).Select(CreateConcurrently));

        Assert.Equal(8, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 100006, 999999));
        Assert.Equal(14, await context.Products.CountAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Products"] = connectionString
                })));
    }
}

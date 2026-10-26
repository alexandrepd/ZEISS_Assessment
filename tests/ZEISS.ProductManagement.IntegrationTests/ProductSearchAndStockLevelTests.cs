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

public sealed class ProductSearchAndStockLevelTests
{
    [Fact]
    public async Task SearchAndStockLevel_FilterProductsAndValidateQueryParameters()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "ProductSearchAndStockLevelTests"
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

        using var searchResponse = await client.GetAsync("/api/products/search?name=OPTICAL");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchResults = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { 100000 },
            searchResults.EnumerateArray().Select(product => product.GetProperty("id").GetInt32()).ToArray());
        Assert.Equal(10, searchResults[0].GetProperty("stock").GetInt32());

        using var noSearchResultsResponse = await client.GetAsync("/api/products/search?name=not-found");
        Assert.Equal(HttpStatusCode.OK, noSearchResultsResponse.StatusCode);
        Assert.Equal("[]", await noSearchResultsResponse.Content.ReadAsStringAsync());

        foreach (var path in new[]
                 {
                     "/api/products/search",
                     "/api/products/search?name=",
                     "/api/products/search?name=%20%20"
                 })
        {
            using var invalidSearchResponse = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.BadRequest, invalidSearchResponse.StatusCode);
            Assert.Equal("application/problem+json", invalidSearchResponse.Content.Headers.ContentType?.MediaType);
            var problem = await invalidSearchResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.True(problem.GetProperty("errors").TryGetProperty("name", out _));
        }

        using var stockRangeResponse = await client.GetAsync("/api/products/stock-level?min=5&max=10");
        Assert.Equal(HttpStatusCode.OK, stockRangeResponse.StatusCode);
        var stockRangeResults = await stockRangeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { 100000, 100001, 100004 },
            stockRangeResults.EnumerateArray().Select(product => product.GetProperty("id").GetInt32()).ToArray());
        Assert.Equal(new[] { 10, 5, 8 },
            stockRangeResults.EnumerateArray().Select(product => product.GetProperty("stock").GetInt32()).ToArray());

        using var exactStockResponse = await client.GetAsync("/api/products/stock-level?min=0&max=0");
        Assert.Equal(HttpStatusCode.OK, exactStockResponse.StatusCode);
        var exactStockResults = await exactStockResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { 100003 },
            exactStockResults.EnumerateArray().Select(product => product.GetProperty("id").GetInt32()).ToArray());
        Assert.Equal(0, exactStockResults[0].GetProperty("stock").GetInt32());

        using var noStockResultsResponse = await client.GetAsync("/api/products/stock-level?min=50&max=60");
        Assert.Equal(HttpStatusCode.OK, noStockResultsResponse.StatusCode);
        Assert.Equal("[]", await noStockResultsResponse.Content.ReadAsStringAsync());

        foreach (var path in new[]
                 {
                     "/api/products/stock-level",
                     "/api/products/stock-level?min=0",
                     "/api/products/stock-level?max=10",
                     "/api/products/stock-level?min=-1&max=10",
                     "/api/products/stock-level?min=10&max=5"
                 })
        {
            using var invalidStockResponse = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.BadRequest, invalidStockResponse.StatusCode);
            Assert.Equal("application/problem+json", invalidStockResponse.Content.Headers.ContentType?.MediaType);
            var problem = await invalidStockResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
        }
    }
}

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

public sealed class StockOperationsTests
{
    [Fact]
    public async Task StockOperations_UpdateStockAndReturnExpectedErrors()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = CreateConnectionString(sqlServer, "StockOperationsTests");
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ProductDbContext(options);
        await context.Database.MigrateAsync();

        await using var factory = CreateFactory(connectionString);
        using var client = factory.CreateClient();

        using var decrementResponse = await client.PostAsync("/api/products/100000/decrement-stock/3", null);
        Assert.Equal(HttpStatusCode.OK, decrementResponse.StatusCode);
        var decremented = await decrementResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, decremented.GetProperty("stock").GetInt32());
        Assert.EndsWith("Z", decremented.GetProperty("updatedAtUtc").GetString());

        using var addResponse = await client.PostAsync("/api/products/100000/add-to-stock/5", null);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var added = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12, added.GetProperty("stock").GetInt32());
        Assert.EndsWith("Z", added.GetProperty("updatedAtUtc").GetString());

        foreach (var path in new[]
                 {
                     "/api/products/100000/decrement-stock/0",
                     "/api/products/100000/decrement-stock/-1",
                     "/api/products/100000/add-to-stock/0"
                 })
        {
            using var invalidResponse = await client.PostAsync(path, null);
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
            Assert.Equal("application/problem+json", invalidResponse.Content.Headers.ContentType?.MediaType);
            var problem = await invalidResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.True(problem.GetProperty("errors").TryGetProperty("quantity", out _));
        }

        using var insufficientResponse = await client.PostAsync("/api/products/100000/decrement-stock/13", null);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal("application/problem+json", insufficientResponse.Content.Headers.ContentType?.MediaType);
        var insufficientProblem = await insufficientResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, insufficientProblem.GetProperty("status").GetInt32());
        Assert.Equal("Stock conflict", insufficientProblem.GetProperty("title").GetString());
        Assert.Equal(12, await context.Products
            .Where(product => product.Id == 100000)
            .Select(product => product.Stock)
            .SingleAsync());

        foreach (var path in new[]
                 {
                     "/api/products/999999/decrement-stock/1",
                     "/api/products/999999/add-to-stock/1"
                 })
        {
            using var missingResponse = await client.PostAsync(path, null);
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
            Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
            var problem = await missingResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(404, problem.GetProperty("status").GetInt32());
            Assert.Equal("Product not found", problem.GetProperty("title").GetString());
        }

        await context.Products.Where(product => product.Id == 100000)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                product => product.Stock,
                int.MaxValue));

        using var overflowResponse = await client.PostAsync("/api/products/100000/add-to-stock/1", null);
        Assert.Equal(HttpStatusCode.Conflict, overflowResponse.StatusCode);
        var overflowProblem = await overflowResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, overflowProblem.GetProperty("status").GetInt32());
        Assert.Equal("Stock conflict", overflowProblem.GetProperty("title").GetString());
        Assert.Equal(int.MaxValue, await context.Products
            .Where(product => product.Id == 100000)
            .Select(product => product.Stock)
            .SingleAsync());
    }

    [Fact]
    public async Task DecrementStock_ConcurrentRequestsDoNotOversell()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = CreateConnectionString(sqlServer, "ConcurrentStockOperationsTests");
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ProductDbContext(options);
        await context.Database.MigrateAsync();
        await context.Products.Where(product => product.Id == 100000)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, 5));

        await using var factory = CreateFactory(connectionString);
        using var client = factory.CreateClient();

        async Task<HttpResponseMessage> DecrementStockAsync()
        {
            return await client.PostAsync("/api/products/100000/decrement-stock/4", null);
        }

        var responses = await Task.WhenAll(DecrementStockAsync(), DecrementStockAsync());

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Equal(1, await context.Products
            .Where(product => product.Id == 100000)
            .Select(product => product.Stock)
            .SingleAsync());
    }

    private static string CreateConnectionString(MsSqlContainer sqlServer, string databaseName)
    {
        return new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;
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

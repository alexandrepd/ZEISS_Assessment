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

public sealed class UpdateProductTests
{
    [Fact]
    public async Task Update_ChangesMetadataAndPreservesStockIncludingChangesAfterEntityWasLoaded()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "UpdateProductTests"
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

        using var response = await client.PutAsJsonAsync("/api/products/100000",
            new { name = " Updated lens ", description = (string?)null, price = 25m, stock = 999 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated lens", updated.GetProperty("name").GetString());
        Assert.Equal(25m, updated.GetProperty("price").GetDecimal());
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("description").ValueKind);
        Assert.Equal(10, updated.GetProperty("stock").GetInt32());
        Assert.EndsWith("Z", updated.GetProperty("updatedAtUtc").GetString());

        using var invalidResponse = await client.PutAsJsonAsync("/api/products/100000",
            new { name = "Invalid change", price = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var tracked = await context.Products.SingleAsync(product => product.Id == 100000);
        Assert.Equal("Updated lens", tracked.Name);
        Assert.Equal(25m, tracked.Price);
        var createdAtUtc = tracked.CreatedAtUtc;

        await using var stockContext = new ProductDbContext(options);
        await stockContext.Products.Where(product => product.Id == 100000)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, 3));

        var commands = new ProductCommands(context);
        var result = await commands.UpdateAsync(100000, "Final lens", "Final description", 30m, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Stock);
        context.ChangeTracker.Clear();
        var saved = await context.Products.SingleAsync(product => product.Id == 100000);
        Assert.Equal(3, saved.Stock);
        Assert.Equal("Final lens", saved.Name);
        Assert.Equal(createdAtUtc, saved.CreatedAtUtc);

        using var missingResponse = await client.PutAsJsonAsync("/api/products/999999",
            new { name = "Missing lens", price = 1m });
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
        var problem = await missingResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal(5, await context.Products.CountAsync());
    }
}

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

public sealed class DeleteProductTests
{
    [Fact]
    public async Task Delete_RemovesOnlyRequestedProductAndReturnsNotFoundWhenMissing()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "DeleteProductTests"
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

        using var response = await client.DeleteAsync("/api/products/100000");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.False(await context.Products.AnyAsync(product => product.Id == 100000));
        Assert.Equal(new[] { 100001, 100002, 100003, 100004 },
            await context.Products.OrderBy(product => product.Id).Select(product => product.Id).ToArrayAsync());
        var remaining = await context.Products.SingleAsync(product => product.Id == 100001);
        Assert.Equal("Microscope", remaining.Name);
        Assert.Equal(1500m, remaining.Price);
        Assert.Equal(5, remaining.Stock);

        using var getResponse = await client.GetAsync("/api/products/100000");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        foreach (var id in new[] { 100000, 999999 })
        {
            using var missingResponse = await client.DeleteAsync($"/api/products/{id}");
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
            Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
            var problem = await missingResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(404, problem.GetProperty("status").GetInt32());
            Assert.Equal("Product not found", problem.GetProperty("title").GetString());
        }

        Assert.Equal(4, await context.Products.CountAsync());
    }
}

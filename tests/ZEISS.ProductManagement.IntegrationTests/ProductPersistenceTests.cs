using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using ZEISS.ProductManagement.Domain;
using ZEISS.ProductManagement.Infrastructure.Persistence;

namespace ZEISS.ProductManagement.IntegrationTests;

public sealed class ProductPersistenceTests
{
    [Fact]
    public async Task Migrations_SeedOnceAndGenerateFirstAvailableProductId()
    {
        await using var sqlServer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718")
            .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
            .Build();
        await sqlServer.StartAsync();

        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = "ProductPersistenceTests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new ProductDbContext(options);

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(
            new[] { 100000, 100001, 100002, 100003, 100004 },
            await context.Products.OrderBy(product => product.Id).Select(product => product.Id).ToArrayAsync());

        var product = new Product("Test lens", null, 25.50m, 3);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        Assert.Equal(100005, product.Id);

        context.ChangeTracker.Clear();
        var saved = await context.Products.SingleAsync(item => item.Id == product.Id);
        Assert.Equal("Test lens", saved.Name);
        Assert.Equal(25.50m, saved.Price);
        Assert.Equal(3, saved.Stock);
        Assert.Equal(6, await context.Products.CountAsync());
    }
}

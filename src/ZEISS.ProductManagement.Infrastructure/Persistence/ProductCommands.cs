using Microsoft.EntityFrameworkCore;
using ZEISS.ProductManagement.Application.Products;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

public sealed class ProductCommands(ProductDbContext context) : IProductCommands
{
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var affectedRows = await context.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    public async Task<ProductStockUpdateResult> DecrementStockAsync(
        int id, int quantity, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var updatedAtUtc = DateTime.UtcNow;
        var affectedRows = await context.Products
            .Where(product => product.Id == id && product.Stock >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    product => product.Stock,
                    product => product.Stock - quantity)
                    .SetProperty(product => product.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        return await GetStockUpdateResultAsync(
            id, affectedRows, cancellationToken);
    }

    public async Task<ProductStockUpdateResult> AddToStockAsync(
        int id, int quantity, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var updatedAtUtc = DateTime.UtcNow;
        var affectedRows = await context.Products
            .Where(product => product.Id == id && product.Stock <= int.MaxValue - quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    product => product.Stock,
                    product => product.Stock + quantity)
                    .SetProperty(product => product.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        return await GetStockUpdateResultAsync(
            id, affectedRows, cancellationToken);
    }

    private async Task<ProductStockUpdateResult> GetStockUpdateResultAsync(
        int id, int affectedRows, CancellationToken cancellationToken)
    {
        if (affectedRows == 0)
        {
            var productExists = await context.Products
                .AnyAsync(product => product.Id == id, cancellationToken);
            if (!productExists)
            {
                return new ProductStockUpdateResult(ProductStockUpdateStatus.ProductNotFound, null);
            }

            return new ProductStockUpdateResult(ProductStockUpdateStatus.Conflict, null);
        }

        var queries = new ProductQueries(context);
        var productResponse = await queries.GetByIdAsync(id, cancellationToken);
        if (productResponse is null)
        {
            return new ProductStockUpdateResult(ProductStockUpdateStatus.ProductNotFound, null);
        }

        return new ProductStockUpdateResult(ProductStockUpdateStatus.Updated, productResponse);
    }

    public async Task<ProductResponse?> UpdateAsync(
        int id, string? name, string? description, decimal price, CancellationToken cancellationToken)
    {
        var product = await context.Products.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.UpdateDetails(name, description, price);
        await context.SaveChangesAsync(cancellationToken);

        var queries = new ProductQueries(context);
        return await queries.GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductResponse> CreateAsync(
        string? name, string? description, decimal price, int stock, CancellationToken cancellationToken)
    {
        var product = new Product(name, description, price, stock);

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}

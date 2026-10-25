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

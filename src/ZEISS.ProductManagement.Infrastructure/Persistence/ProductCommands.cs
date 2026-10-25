using ZEISS.ProductManagement.Application.Products;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

public sealed class ProductCommands(ProductDbContext context) : IProductCommands
{
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

using Microsoft.EntityFrameworkCore;
using ZEISS.ProductManagement.Application.Products;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

public sealed class ProductQueries(ProductDbContext context) : IProductQueries
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var query = context.Products
            .OrderBy(product => product.Id);

        var projectedQuery = ProjectProducts(query);

        return await projectedQuery.ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var query = context.Products
            .Where(product => product.Id == id);

        var projectedQuery = ProjectProducts(query);

        return await projectedQuery.SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ProductResponse> ProjectProducts(IQueryable<Product> products)
    {
        return products.AsNoTracking()
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                DateTime.SpecifyKind(product.CreatedAtUtc, DateTimeKind.Utc),
                product.UpdatedAtUtc.HasValue
                    ? DateTime.SpecifyKind(product.UpdatedAtUtc.Value, DateTimeKind.Utc)
                    : null));
    }
}

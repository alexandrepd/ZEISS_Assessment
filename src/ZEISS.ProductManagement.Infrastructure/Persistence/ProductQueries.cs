using Microsoft.EntityFrameworkCore;
using ZEISS.ProductManagement.Application.Products;

namespace ZEISS.ProductManagement.Infrastructure.Persistence;

public sealed class ProductQueries(ProductDbContext context) : IProductQueries
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await context.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                DateTime.SpecifyKind(product.CreatedAtUtc, DateTimeKind.Utc),
                product.UpdatedAtUtc.HasValue
                    ? DateTime.SpecifyKind(product.UpdatedAtUtc.Value, DateTimeKind.Utc)
                    : null))
            .ToListAsync(cancellationToken);
    }
}

namespace ZEISS.ProductManagement.Application.Products;

public interface IProductQueries
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
}

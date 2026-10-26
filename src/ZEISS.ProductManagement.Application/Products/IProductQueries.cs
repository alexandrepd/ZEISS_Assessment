namespace ZEISS.ProductManagement.Application.Products;

public interface IProductQueries
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductResponse>> SearchByNameAsync(
        string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductResponse>> ListByStockLevelAsync(
        int min, int max, CancellationToken cancellationToken);
}

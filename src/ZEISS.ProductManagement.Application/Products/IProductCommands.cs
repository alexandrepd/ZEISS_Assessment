namespace ZEISS.ProductManagement.Application.Products;

public interface IProductCommands
{
    Task<ProductResponse> CreateAsync(
        string? name, string? description, decimal price, int stock, CancellationToken cancellationToken);
    Task<ProductResponse?> UpdateAsync(
        int id, string? name, string? description, decimal price, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<ProductStockUpdateResult> DecrementStockAsync(
        int id, int quantity, CancellationToken cancellationToken);
    Task<ProductStockUpdateResult> AddToStockAsync(
        int id, int quantity, CancellationToken cancellationToken);
}

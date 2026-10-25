namespace ZEISS.ProductManagement.Application.Products;

public interface IProductCommands
{
    Task<ProductResponse> CreateAsync(
        string? name, string? description, decimal price, int stock, CancellationToken cancellationToken);
}

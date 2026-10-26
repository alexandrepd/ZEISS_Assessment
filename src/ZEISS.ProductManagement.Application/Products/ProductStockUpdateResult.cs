namespace ZEISS.ProductManagement.Application.Products;

public enum ProductStockUpdateStatus
{
    Updated,
    ProductNotFound,
    Conflict
}

public sealed record ProductStockUpdateResult(
    ProductStockUpdateStatus Status,
    ProductResponse? Product);

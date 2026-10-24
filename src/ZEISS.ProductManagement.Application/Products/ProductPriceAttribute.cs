using System.ComponentModel.DataAnnotations;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Application.Products;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class ProductPriceAttribute : ValidationAttribute
{
    public ProductPriceAttribute()
        : base("Price must be positive, fit decimal(18,2), and have at most two decimal places.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null ||
        value is decimal price &&
        price > 0 &&
        price <= Product.MaxPrice &&
        decimal.Round(price, 2) == price;
}

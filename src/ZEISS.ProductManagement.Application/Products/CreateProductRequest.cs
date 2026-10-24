using System.ComponentModel.DataAnnotations;
using ZEISS.ProductManagement.Domain;

namespace ZEISS.ProductManagement.Application.Products;

public sealed record CreateProductRequest
{
    [Required]
    [StringLength(Product.MaxNameLength)]
    public string? Name
    {
        get;
        init => field = value?.Trim();
    }

    [StringLength(Product.MaxDescriptionLength)]
    public string? Description { get; init; }

    [Required]
    [ProductPrice]
    public decimal? Price { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }
}

namespace ZEISS.ProductManagement.Domain;

public sealed class Product
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const decimal MaxPrice = 9_999_999_999_999_999.99m;

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public Product(string? name, string? description, decimal price, int stock)
    {
        var normalizedName = ValidateDetails(name, description, price);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);

        Name = normalizedName;
        Description = description;
        Price = price;
        Stock = stock;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string? name, string? description, decimal price)
    {
        var normalizedName = ValidateDetails(name, description, price);

        Name = normalizedName;
        Description = description;
        Price = price;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string ValidateDetails(string? name, string? description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Name must not exceed {MaxNameLength} characters.", nameof(name));
        }

        if (description is not null && description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description must not exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        if (price <= 0 || price > MaxPrice || decimal.Round(price, 2) != price)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price), "Price must be positive, fit decimal(18,2), and have at most two decimal places.");
        }

        return normalizedName;
    }
}

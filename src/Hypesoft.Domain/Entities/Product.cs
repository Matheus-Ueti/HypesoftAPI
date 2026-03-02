using Hypesoft.Domain.Exceptions;

namespace Hypesoft.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string CategoryId { get; private set; } = string.Empty;
    public int Stock { get; private set; }

    private Product() { }

    public static Product Create(string name, string description, decimal price, string categoryId, int stock)
    {
        if (price < 0) throw new DomainException("Price cannot be negative.");
        if (stock < 0) throw new DomainException("Stock cannot be negative.");

        return new Product
        {
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            Stock = stock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description, decimal price, string categoryId, int stock)
    {
        if (price < 0) throw new DomainException("Price cannot be negative.");
        if (stock < 0) throw new DomainException("Stock cannot be negative.");

        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }
}

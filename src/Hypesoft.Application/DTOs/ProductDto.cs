namespace Hypesoft.Application.DTOs;

public record ProductDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string CategoryId,
    int Stock,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

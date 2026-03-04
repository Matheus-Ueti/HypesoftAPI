using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Commands;

public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
public record UpdateCategoryCommand(string Id, string Name, string? Description) : IRequest<CategoryDto>;
public record DeleteCategoryCommand(string Id) : IRequest;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string CategoryId,
    int Stock
) : IRequest<ProductDto>;

public record UpdateProductCommand(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string CategoryId,
    int Stock
) : IRequest<ProductDto>;

public record DeleteProductCommand(string Id) : IRequest;
public record UpdateStockCommand(string Id, int Stock) : IRequest<ProductDto>;

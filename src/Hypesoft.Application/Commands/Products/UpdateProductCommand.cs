using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Commands.Products;

public record UpdateProductCommand(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string CategoryId,
    int Stock
) : IRequest<ProductDto>;

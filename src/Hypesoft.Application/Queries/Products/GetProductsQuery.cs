using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries.Products;

public record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;

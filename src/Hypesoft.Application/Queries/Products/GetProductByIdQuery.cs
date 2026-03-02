using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries.Products;

public record GetProductByIdQuery(string Id) : IRequest<ProductDto>;

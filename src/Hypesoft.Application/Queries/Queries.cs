using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;
public record GetCategoryByIdQuery(string Id) : IRequest<CategoryDto>;

public record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;
public record GetProductByIdQuery(string Id) : IRequest<ProductDto>;

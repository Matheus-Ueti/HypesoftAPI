using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;
public record GetCategoryByIdQuery(string Id) : IRequest<CategoryDto>;

public record GetProductsQuery(
    string? Name = null,
    string? CategoryId = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<ProductDto>>;

public record GetProductByIdQuery(string Id) : IRequest<ProductDto>;
public record GetLowStockProductsQuery : IRequest<IEnumerable<ProductDto>>;
public record GetDashboardQuery : IRequest<DashboardDto>;

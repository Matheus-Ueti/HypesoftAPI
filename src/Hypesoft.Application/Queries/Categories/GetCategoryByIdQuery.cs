using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries.Categories;

public record GetCategoryByIdQuery(string Id) : IRequest<CategoryDto>;

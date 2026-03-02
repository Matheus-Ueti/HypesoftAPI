using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Queries.Categories;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;

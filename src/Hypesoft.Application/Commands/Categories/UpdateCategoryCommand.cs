using MediatR;
using Hypesoft.Application.DTOs;

namespace Hypesoft.Application.Commands.Categories;

public record UpdateCategoryCommand(string Id, string Name, string? Description) : IRequest<CategoryDto>;

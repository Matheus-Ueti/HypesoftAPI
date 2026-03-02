using MediatR;

namespace Hypesoft.Application.Commands.Categories;

public record DeleteCategoryCommand(string Id) : IRequest;

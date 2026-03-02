using MediatR;

namespace Hypesoft.Application.Commands.Products;

public record DeleteProductCommand(string Id) : IRequest;

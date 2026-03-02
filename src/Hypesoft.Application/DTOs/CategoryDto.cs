namespace Hypesoft.Application.DTOs;

public record CategoryDto(
    string Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);

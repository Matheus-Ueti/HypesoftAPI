namespace Hypesoft.Application.DTOs;

public record DashboardDto(
    long TotalProducts,
    decimal TotalStockValue,
    IEnumerable<ProductDto> LowStockProducts,
    IEnumerable<CategoryStockDto> ProductsByCategory
);

public record CategoryStockDto(string CategoryId, int Count);

public record PagedResult<T>(
    IEnumerable<T> Items,
    long Total,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

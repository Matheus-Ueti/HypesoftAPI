using Hypesoft.Domain.Entities;

namespace Hypesoft.Domain.Repositories;

public interface IRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> ExistsByNameAsync(string name);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId);
}

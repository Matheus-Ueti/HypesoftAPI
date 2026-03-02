using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Repositories;
using Hypesoft.Infrastructure.Data;

namespace Hypesoft.Infrastructure.Repositories;

public abstract class BaseMongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;

    protected BaseMongoRepository(MongoDbContext context, string collectionName)
    {
        _collection = context.GetCollection<T>(collectionName);
    }

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<T?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<T> CreateAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(T entity) =>
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(x => x.Id == id);
}

public class CategoryRepository : BaseMongoRepository<Category>, ICategoryRepository
{
    public CategoryRepository(MongoDbContext context)
        : base(context, "categories") { }

    public async Task<bool> ExistsByNameAsync(string name) =>
        await _collection.Find(x => x.Name == name).AnyAsync();
}

public class ProductRepository : BaseMongoRepository<Product>, IProductRepository
{
    public ProductRepository(MongoDbContext context)
        : base(context, "products") { }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId) =>
        await _collection.Find(x => x.CategoryId == categoryId).ToListAsync();
}

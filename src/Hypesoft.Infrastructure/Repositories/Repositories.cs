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

    public async Task<(IEnumerable<Product> Items, long Total)> SearchAsync(
        string? name, string? categoryId, int page, int pageSize)
    {
        var builder = Builders<Product>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(name))
            filter &= builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(name, "i"));

        if (!string.IsNullOrWhiteSpace(categoryId))
            filter &= builder.Eq(x => x.CategoryId, categoryId);

        var total = await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 10) =>
        await _collection.Find(x => x.Stock < threshold).ToListAsync();

    public async Task<long> CountAsync() =>
        await _collection.CountDocumentsAsync(_ => true);

    public async Task<decimal> GetTotalStockValueAsync()
    {
        var products = await _collection.Find(_ => true).ToListAsync();
        return products.Sum(p => p.Price * p.Stock);
    }

    public async Task<IEnumerable<(string CategoryId, int Count)>> GetCountByCategoryAsync()
    {
        var products = await _collection.Find(_ => true).ToListAsync();
        return products
            .GroupBy(p => p.CategoryId)
            .Select(g => (g.Key, g.Count()));
    }
}

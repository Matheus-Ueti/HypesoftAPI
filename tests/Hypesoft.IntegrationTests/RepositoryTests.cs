using FluentAssertions;
using Microsoft.Extensions.Options;
using Testcontainers.MongoDb;
using Xunit;
using Hypesoft.Domain.Entities;
using Hypesoft.Infrastructure.Data;
using Hypesoft.Infrastructure.Repositories;

namespace Hypesoft.IntegrationTests;

/// <summary>
/// Shared fixture that starts a MongoDB container once per test collection.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().Build();

    public MongoDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName     = "hypesoft_integration_tests"
        });

        Context = new MongoDbContext(settings);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Mongo")]
public class MongoCollection : ICollectionFixture<MongoFixture> { }

// ---------------------------------------------------------------------------
// CategoryRepository Integration Tests
// ---------------------------------------------------------------------------

[Collection("Mongo")]
public class CategoryRepositoryTests
{
    private readonly CategoryRepository _repo;

    public CategoryRepositoryTests(MongoFixture fixture)
        => _repo = new CategoryRepository(fixture.Context);

    [Fact]
    public async Task CreateAsync_ShouldPersistCategory()
    {
        var cat = Category.Create("Eletrônicos", "Dispositivos");
        await _repo.CreateAsync(cat);

        var found = await _repo.GetByIdAsync(cat.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Eletrônicos");
        found.Description.Should().Be("Dispositivos");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPersistedCategories()
    {
        await _repo.CreateAsync(Category.Create("Cat-A", null));
        await _repo.CreateAsync(Category.Create("Cat-B", null));

        var all = await _repo.GetAllAsync();

        all.Should().Contain(c => c.Name == "Cat-A");
        all.Should().Contain(c => c.Name == "Cat-B");
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeCategoryData()
    {
        var cat = Category.Create("Antes", null);
        await _repo.CreateAsync(cat);

        cat.Update("Depois", "Nova desc");
        await _repo.UpdateAsync(cat);

        var updated = await _repo.GetByIdAsync(cat.Id);
        updated!.Name.Should().Be("Depois");
        updated.Description.Should().Be("Nova desc");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory()
    {
        var cat = Category.Create("Deletar", null);
        await _repo.CreateAsync(cat);

        await _repo.DeleteAsync(cat.Id);

        var found = await _repo.GetByIdAsync(cat.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var found = await _repo.GetByIdAsync("id-que-nao-existe");
        found.Should().BeNull();
    }
}

// ---------------------------------------------------------------------------
// ProductRepository Integration Tests
// ---------------------------------------------------------------------------

[Collection("Mongo")]
public class ProductRepositoryTests
{
    private readonly ProductRepository _repo;

    public ProductRepositoryTests(MongoFixture fixture)
        => _repo = new ProductRepository(fixture.Context);

    [Fact]
    public async Task CreateAsync_ShouldPersistProduct()
    {
        var product = Product.Create("Notebook Gamer", "Intel i7", 4999m, "cat-01", 10);
        await _repo.CreateAsync(product);

        var found = await _repo.GetByIdAsync(product.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Notebook Gamer");
        found.Price.Should().Be(4999m);
        found.Stock.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeProductData()
    {
        var product = Product.Create("Antigo", "Desc", 100m, "cat-01", 5);
        await _repo.CreateAsync(product);

        product.Update("Novo", "Nova desc", 200m, "cat-02", 15);
        await _repo.UpdateAsync(product);

        var updated = await _repo.GetByIdAsync(product.Id);
        updated!.Name.Should().Be("Novo");
        updated.Price.Should().Be(200m);
        updated.Stock.Should().Be(15);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct()
    {
        var product = Product.Create("Deletar", "Desc", 10m, "cat", 1);
        await _repo.CreateAsync(product);

        await _repo.DeleteAsync(product.Id);

        var found = await _repo.GetByIdAsync(product.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByName_CaseInsensitive()
    {
        await _repo.CreateAsync(Product.Create("Mouse Gamer", "Desc", 200m, "cat", 10));
        await _repo.CreateAsync(Product.Create("Teclado Mecânico", "Desc", 300m, "cat", 5));

        var (items, total) = await _repo.SearchAsync("mouse", null, 1, 10);

        items.Should().HaveCount(1);
        items.First().Name.Should().Be("Mouse Gamer");
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByCategory()
    {
        await _repo.CreateAsync(Product.Create("Produto A", "Desc", 100m, "cat-X", 5));
        await _repo.CreateAsync(Product.Create("Produto B", "Desc", 100m, "cat-Y", 5));

        var (items, total) = await _repo.SearchAsync(null, "cat-X", 1, 10);

        items.Should().OnlyContain(p => p.CategoryId == "cat-X");
        total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchAsync_ShouldRespectPagination()
    {
        for (int i = 0; i < 5; i++)
            await _repo.CreateAsync(Product.Create($"Paginated-{i}", "Desc", 10m, "cat-page", i + 1));

        var (page1, total) = await _repo.SearchAsync(null, "cat-page", 1, 2);
        var (page2, _)     = await _repo.SearchAsync(null, "cat-page", 2, 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        total.Should().Be(5);
    }

    [Fact]
    public async Task GetLowStockAsync_ShouldReturnOnlyProductsBelowThreshold()
    {
        await _repo.CreateAsync(Product.Create("LowA",  "Desc", 10m, "cat-ls", 3));
        await _repo.CreateAsync(Product.Create("LowB",  "Desc", 10m, "cat-ls", 9));
        await _repo.CreateAsync(Product.Create("HighC", "Desc", 10m, "cat-ls", 20));

        var lowStock = await _repo.GetLowStockAsync(threshold: 10);

        lowStock.Should().OnlyContain(p => p.Stock < 10);
        lowStock.Should().Contain(p => p.Name == "LowA");
        lowStock.Should().Contain(p => p.Name == "LowB");
        lowStock.Should().NotContain(p => p.Name == "HighC");
    }
}

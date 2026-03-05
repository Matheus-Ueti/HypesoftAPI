using AutoMapper;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using Hypesoft.Application.Commands;
using Hypesoft.Application.DTOs;
using Hypesoft.Application.Handlers;
using Hypesoft.Application.Mappings;
using Hypesoft.Application.Queries;
using Hypesoft.Application.Validators;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Exceptions;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Tests;

// ---------------------------------------------------------------------------
// Category Query Handlers
// ---------------------------------------------------------------------------

public class GetCategoriesHandlerTests
{
    private readonly ICategoryRepository _repo   = Substitute.For<ICategoryRepository>();
    private readonly IMemoryCache        _cache  = new MemoryCache(new MemoryCacheOptions());
    private readonly IMapper             _mapper;

    public GetCategoriesHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnAllCategories()
    {
        var cats = new[] { Category.Create("A", null), Category.Create("B", "Desc") };
        _repo.GetAllAsync().Returns(cats);

        var result = await new GetCategoriesHandler(_repo, _mapper, _cache)
            .Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().BeEquivalentTo(["A", "B"]);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedResult_OnSecondCall()
    {
        var cats = new[] { Category.Create("A", null) };
        _repo.GetAllAsync().Returns(cats);

        var handler = new GetCategoriesHandler(_repo, _mapper, _cache);
        await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Repository should be called only once — second call served from cache
        await _repo.Received(1).GetAllAsync();
    }
}

public class GetCategoryByIdHandlerTests
{
    private readonly ICategoryRepository _repo   = Substitute.For<ICategoryRepository>();
    private readonly IMapper             _mapper;

    public GetCategoryByIdHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryDto_WhenFound()
    {
        var cat = Category.Create("Eletrônicos", "Desc");
        _repo.GetByIdAsync(cat.Id).Returns(cat);

        var result = await new GetCategoryByIdHandler(_repo, _mapper)
            .Handle(new GetCategoryByIdQuery(cat.Id), CancellationToken.None);

        result.Id.Should().Be(cat.Id);
        result.Name.Should().Be("Eletrônicos");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var act = () => new GetCategoryByIdHandler(_repo, _mapper)
            .Handle(new GetCategoryByIdQuery("nao-existe"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

// ---------------------------------------------------------------------------
// Product Query Handlers
// ---------------------------------------------------------------------------

public class GetProductByIdHandlerTests
{
    private readonly IProductRepository _repo   = Substitute.For<IProductRepository>();
    private readonly IMapper            _mapper;

    public GetProductByIdHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto_WhenFound()
    {
        var product = Product.Create("Notebook", "Gamer", 4999m, "cat-01", 10);
        _repo.GetByIdAsync(product.Id).Returns(product);

        var result = await new GetProductByIdHandler(_repo, _mapper)
            .Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Id.Should().Be(product.Id);
        result.Name.Should().Be("Notebook");
        result.Price.Should().Be(4999m);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var act = () => new GetProductByIdHandler(_repo, _mapper)
            .Handle(new GetProductByIdQuery("nao-existe"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetProductsHandlerTests
{
    private readonly IProductRepository _repo   = Substitute.For<IProductRepository>();
    private readonly IMemoryCache       _cache  = new MemoryCache(new MemoryCacheOptions());
    private readonly IMapper            _mapper;

    public GetProductsHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult_WithCorrectData()
    {
        var products = new[] { Product.Create("A", "Desc", 10m, "cat", 5) };
        _repo.SearchAsync(null, null, 1, 10).Returns((products.AsEnumerable(), 1L));

        var result = await new GetProductsHandler(_repo, _mapper, _cache)
            .Handle(new GetProductsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedResult_OnSecondCall()
    {
        var products = new[] { Product.Create("A", "Desc", 10m, "cat", 5) };
        _repo.SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
             .Returns((products.AsEnumerable(), 1L));

        var handler = new GetProductsHandler(_repo, _mapper, _cache);
        await handler.Handle(new GetProductsQuery(), CancellationToken.None);
        await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Repository called only once — second call served from cache
        await _repo.Received(1).SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>());
    }
}

public class GetLowStockProductsHandlerTests
{
    private readonly IProductRepository _repo   = Substitute.For<IProductRepository>();
    private readonly IMapper            _mapper;

    public GetLowStockProductsHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyLowStockProducts()
    {
        var low  = Product.Create("LowStock", "Desc", 10m, "cat", 3);
        _repo.GetLowStockAsync().Returns(new[] { low });

        var result = await new GetLowStockProductsHandler(_repo, _mapper)
            .Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("LowStock");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoLowStockProducts()
    {
        _repo.GetLowStockAsync().Returns(Array.Empty<Product>());

        var result = await new GetLowStockProductsHandler(_repo, _mapper)
            .Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetDashboardHandlerTests
{
    private readonly IProductRepository _repo   = Substitute.For<IProductRepository>();
    private readonly IMapper            _mapper;

    public GetDashboardHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectMetrics()
    {
        var products = new[]
        {
            Product.Create("A", "Desc", 100m, "cat-1", 5),   // low stock
            Product.Create("B", "Desc", 200m, "cat-1", 20),
            Product.Create("C", "Desc", 50m,  "cat-2", 3),   // low stock
        };
        _repo.GetAllAsync().Returns(products);

        var result = await new GetDashboardHandler(_repo, _mapper)
            .Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(3);
        result.TotalStockValue.Should().Be(100m * 5 + 200m * 20 + 50m * 3);  // 4650
        result.LowStockProducts.Should().HaveCount(2);
        result.ProductsByCategory.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeros_WhenNoProducts()
    {
        _repo.GetAllAsync().Returns(Array.Empty<Product>());

        var result = await new GetDashboardHandler(_repo, _mapper)
            .Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(0);
        result.TotalStockValue.Should().Be(0);
        result.LowStockProducts.Should().BeEmpty();
        result.ProductsByCategory.Should().BeEmpty();
    }
}

// ---------------------------------------------------------------------------
// Additional Validator Tests
// ---------------------------------------------------------------------------

public class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
        => _validator.TestValidate(new UpdateProductCommand("id", "", "Desc", 10m, "cat", 1))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Should_HaveError_WhenDescriptionIsEmpty()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "", 10m, "cat", 1))
            .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void Should_HaveError_WhenPriceIsNegative()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "Desc", -1m, "cat", 1))
            .ShouldHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Should_HaveError_WhenCategoryIdIsEmpty()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "Desc", 10m, "", 1))
            .ShouldHaveValidationErrorFor(x => x.CategoryId);

    [Fact]
    public void Should_HaveError_WhenStockIsNegative()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "Desc", 10m, "cat", -1))
            .ShouldHaveValidationErrorFor(x => x.Stock);

    [Fact]
    public void Should_NotHaveErrors_WhenDataIsValid()
        => _validator.TestValidate(new UpdateProductCommand("id", "Notebook", "Gamer", 4999m, "cat-1", 10))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Should_AllowZeroPrice()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "Desc", 0m, "cat", 1))
            .ShouldNotHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Should_AllowZeroStock()
        => _validator.TestValidate(new UpdateProductCommand("id", "Nome", "Desc", 10m, "cat", 0))
            .ShouldNotHaveValidationErrorFor(x => x.Stock);
}

public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenIdIsEmpty()
        => _validator.TestValidate(new UpdateCategoryCommand("", "Nome", null))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
        => _validator.TestValidate(new UpdateCategoryCommand("id-1", "", null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Should_NotHaveErrors_WhenDataIsValid()
        => _validator.TestValidate(new UpdateCategoryCommand("id-1", "Eletrônicos", "Desc"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Should_AllowNullDescription()
        => _validator.TestValidate(new UpdateCategoryCommand("id-1", "Eletrônicos", null))
            .ShouldNotHaveAnyValidationErrors();
}

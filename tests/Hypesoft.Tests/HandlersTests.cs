using AutoMapper;
using FluentAssertions;
using FluentValidation;
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
// Category Handlers
// ---------------------------------------------------------------------------

public class CreateCategoryHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();
    private readonly IMapper             _mapper;

    public CreateCategoryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryDto_WithCorrectData()
    {
        var command = new CreateCategoryCommand("Eletrônicos", "Dispositivos eletrônicos");
        _repository.CreateAsync(Arg.Any<Category>()).Returns(c => c.Arg<Category>());

        var result = await new CreateCategoryHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        result.Name.Should().Be("Eletrônicos");
        result.Description.Should().Be("Dispositivos eletrônicos");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_ExactlyOnce()
    {
        var command = new CreateCategoryCommand("Eletrônicos", null);
        _repository.CreateAsync(Arg.Any<Category>()).Returns(c => c.Arg<Category>());

        await new CreateCategoryHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        await _repository.Received(1).CreateAsync(Arg.Any<Category>());
    }
}

public class UpdateCategoryHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();
    private readonly IMapper             _mapper;

    public UpdateCategoryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndReturnDto()
    {
        var existing = Category.Create("Antiga", null);
        _repository.GetByIdAsync(existing.Id).Returns(existing);
        _repository.UpdateAsync(Arg.Any<Category>()).Returns(Task.CompletedTask);

        var command = new UpdateCategoryCommand(existing.Id, "Nova", "Desc nova");
        var result  = await new UpdateCategoryHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        result.Name.Should().Be("Nova");
        result.Description.Should().Be("Desc nova");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var command = new UpdateCategoryCommand("nao-existe", "X", null);
        var act     = () => new UpdateCategoryHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteCategoryHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();

    [Fact]
    public async Task Handle_ShouldCallDeleteAsync_WhenCategoryExists()
    {
        var existing = Category.Create("Cat", null);
        _repository.GetByIdAsync(existing.Id).Returns(existing);
        _repository.DeleteAsync(existing.Id).Returns(Task.CompletedTask);

        await new DeleteCategoryHandler(_repository, _cache)
            .Handle(new DeleteCategoryCommand(existing.Id), CancellationToken.None);

        await _repository.Received(1).DeleteAsync(existing.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var act = () => new DeleteCategoryHandler(_repository, _cache)
            .Handle(new DeleteCategoryCommand("nao-existe"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

// ---------------------------------------------------------------------------
// Product Handlers
// ---------------------------------------------------------------------------

public class CreateProductHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();
    private readonly IMapper            _mapper;

    public CreateProductHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto_WithCorrectData()
    {
        var command = new CreateProductCommand("Notebook", "Gamer", 4999.99m, "cat-01", 10);
        _repository.CreateAsync(Arg.Any<Product>()).Returns(c => c.Arg<Product>());

        var result = await new CreateProductHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        result.Name.Should().Be("Notebook");
        result.Price.Should().Be(4999.99m);
        result.Stock.Should().Be(10);
        result.CategoryId.Should().Be("cat-01");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_ExactlyOnce()
    {
        var command = new CreateProductCommand("Notebook", "Gamer", 4999.99m, "cat-01", 10);
        _repository.CreateAsync(Arg.Any<Product>()).Returns(c => c.Arg<Product>());

        await new CreateProductHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        await _repository.Received(1).CreateAsync(Arg.Any<Product>());
    }
}

public class UpdateProductHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();
    private readonly IMapper            _mapper;

    public UpdateProductHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndReturnDto()
    {
        var existing = Product.Create("Antigo", "Desc", 100m, "cat-01", 5);
        _repository.GetByIdAsync(existing.Id).Returns(existing);
        _repository.UpdateAsync(Arg.Any<Product>()).Returns(Task.CompletedTask);

        var command = new UpdateProductCommand(existing.Id, "Novo", "Nova desc", 200m, "cat-02", 10);
        var result  = await new UpdateProductHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        result.Name.Should().Be("Novo");
        result.Price.Should().Be(200m);
        result.Stock.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var command = new UpdateProductCommand("nao-existe", "X", "X", 1m, "cat", 1);
        var act     = () => new UpdateProductHandler(_repository, _mapper, _cache)
            .Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteProductHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IMemoryCache        _cache      = Substitute.For<IMemoryCache>();

    [Fact]
    public async Task Handle_ShouldCallDeleteAsync_WhenProductExists()
    {
        var existing = Product.Create("Prod", "Desc", 10m, "cat", 1);
        _repository.GetByIdAsync(existing.Id).Returns(existing);
        _repository.DeleteAsync(existing.Id).Returns(Task.CompletedTask);

        await new DeleteProductHandler(_repository, _cache)
            .Handle(new DeleteProductCommand(existing.Id), CancellationToken.None);

        await _repository.Received(1).DeleteAsync(existing.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var act = () => new DeleteProductHandler(_repository, _cache)
            .Handle(new DeleteProductCommand("nao-existe"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class UpdateStockHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IMapper            _mapper;

    public UpdateStockHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldUpdateStock_AndReturnDto()
    {
        var existing = Product.Create("Prod", "Desc", 10m, "cat", 5);
        _repository.GetByIdAsync(existing.Id).Returns(existing);
        _repository.UpdateAsync(Arg.Any<Product>()).Returns(Task.CompletedTask);

        var result = await new UpdateStockHandler(_repository, _mapper)
            .Handle(new UpdateStockCommand(existing.Id, 99), CancellationToken.None);

        result.Stock.Should().Be(99);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var act = () => new UpdateStockHandler(_repository, _mapper)
            .Handle(new UpdateStockCommand("nao-existe", 10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

// ---------------------------------------------------------------------------
// Domain Entity Tests
// ---------------------------------------------------------------------------

public class ProductDomainTests
{
    [Fact]
    public void Create_ShouldThrowDomainException_WhenPriceIsNegative()
    {
        var act = () => Product.Create("Prod", "Desc", -1m, "cat", 0);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenStockIsNegative()
    {
        var act = () => Product.Create("Prod", "Desc", 10m, "cat", -1);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void UpdateStock_ShouldThrowDomainException_WhenStockIsNegative()
    {
        var product = Product.Create("Prod", "Desc", 10m, "cat", 5);
        var act     = () => product.UpdateStock(-1);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var product = Product.Create("Prod", "Desc", 10m, "cat", 5);
        product.Name.Should().Be("Prod");
        product.Price.Should().Be(10m);
        product.Stock.Should().Be(5);
    }
}

public class CategoryDomainTests
{
    [Fact]
    public void Create_ShouldReturnCategory_WithCorrectData()
    {
        var cat = Category.Create("Eletro", "Desc");
        cat.Name.Should().Be("Eletro");
        cat.Description.Should().Be("Desc");
        cat.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        var cat = Category.Create("Antigo", null);
        cat.Update("Novo", "Nova desc");
        cat.Name.Should().Be("Novo");
        cat.Description.Should().Be("Nova desc");
    }
}

// ---------------------------------------------------------------------------
// Validator Tests
// ---------------------------------------------------------------------------

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
        => _validator.TestValidate(new CreateProductCommand("", "Desc", 10m, "cat", 1))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Should_HaveError_WhenPriceIsNegative()
        => _validator.TestValidate(new CreateProductCommand("Nome", "Desc", -1m, "cat", 1))
            .ShouldHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Should_HaveError_WhenStockIsNegative()
        => _validator.TestValidate(new CreateProductCommand("Nome", "Desc", 10m, "cat", -1))
            .ShouldHaveValidationErrorFor(x => x.Stock);

    [Fact]
    public void Should_HaveError_WhenCategoryIdIsEmpty()
        => _validator.TestValidate(new CreateProductCommand("Nome", "Desc", 10m, "", 1))
            .ShouldHaveValidationErrorFor(x => x.CategoryId);

    [Fact]
    public void Should_NotHaveErrors_WhenDataIsValid()
        => _validator.TestValidate(new CreateProductCommand("Nome", "Desc", 10m, "cat-1", 5))
            .ShouldNotHaveAnyValidationErrors();
}

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
        => _validator.TestValidate(new CreateCategoryCommand("", null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Should_NotHaveErrors_WhenNameIsValid()
        => _validator.TestValidate(new CreateCategoryCommand("Eletrônicos", null))
            .ShouldNotHaveAnyValidationErrors();
}

public class UpdateStockValidatorTests
{
    private readonly UpdateStockValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenStockIsNegative()
        => _validator.TestValidate(new UpdateStockCommand("id-1", -5))
            .ShouldHaveValidationErrorFor(x => x.Stock);

    [Fact]
    public void Should_NotHaveErrors_WhenDataIsValid()
        => _validator.TestValidate(new UpdateStockCommand("id-1", 10))
            .ShouldNotHaveAnyValidationErrors();
}

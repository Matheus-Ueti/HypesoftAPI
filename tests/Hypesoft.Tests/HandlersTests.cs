using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Hypesoft.Application.Commands;
using Hypesoft.Application.DTOs;
using Hypesoft.Application.Handlers;
using Hypesoft.Application.Mappings;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Tests;

public class CreateCategoryHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly IMapper _mapper;

    public CreateCategoryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryDto_WithCorrectData()
    {
        // Arrange
        var command = new CreateCategoryCommand("Eletrônicos", "Dispositivos eletrônicos");
        _repository.CreateAsync(Arg.Any<Domain.Entities.Category>())
            .Returns(callInfo => callInfo.Arg<Domain.Entities.Category>());

        var handler = new CreateCategoryHandler(_repository, _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Eletrônicos");
        result.Description.Should().Be("Dispositivos eletrônicos");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        var command = new CreateCategoryCommand("Eletrônicos", null);
        _repository.CreateAsync(Arg.Any<Domain.Entities.Category>())
            .Returns(callInfo => callInfo.Arg<Domain.Entities.Category>());

        var handler = new CreateCategoryHandler(_repository, _mapper);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).CreateAsync(Arg.Any<Domain.Entities.Category>());
    }
}

public class CreateProductHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IMapper _mapper;

    public CreateProductHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto_WithCorrectData()
    {
        // Arrange
        var command = new CreateProductCommand("Notebook", "Notebook gamer", 4999.99m, "cat-01", 10);
        _repository.CreateAsync(Arg.Any<Domain.Entities.Product>())
            .Returns(callInfo => callInfo.Arg<Domain.Entities.Product>());

        var handler = new CreateProductHandler(_repository, _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Notebook");
        result.Price.Should().Be(4999.99m);
        result.Stock.Should().Be(10);
        result.CategoryId.Should().Be("cat-01");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        var command = new CreateProductCommand("Notebook", "Notebook gamer", 4999.99m, "cat-01", 10);
        _repository.CreateAsync(Arg.Any<Domain.Entities.Product>())
            .Returns(callInfo => callInfo.Arg<Domain.Entities.Product>());

        var handler = new CreateProductHandler(_repository, _mapper);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).CreateAsync(Arg.Any<Domain.Entities.Product>());
    }
}

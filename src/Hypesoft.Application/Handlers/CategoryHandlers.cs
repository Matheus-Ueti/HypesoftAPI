using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Hypesoft.Application.Commands;
using Hypesoft.Application.DTOs;
using Hypesoft.Application.Queries;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Exceptions;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Application.Handlers;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public CreateCategoryHandler(ICategoryRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper     = mapper;
        _cache      = cache;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.Description);
        await _repository.CreateAsync(category);
        _cache.Remove(GetCategoriesHandler.CacheKey);
        return _mapper.Map<CategoryDto>(category);
    }
}

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public UpdateCategoryHandler(ICategoryRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper     = mapper;
        _cache      = cache;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        category.Update(request.Name, request.Description);
        await _repository.UpdateAsync(category);
        _cache.Remove(GetCategoriesHandler.CacheKey);
        return _mapper.Map<CategoryDto>(category);
    }
}

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IMemoryCache _cache;

    public DeleteCategoryHandler(ICategoryRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _ = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        await _repository.DeleteAsync(request.Id);
        _cache.Remove(GetCategoriesHandler.CacheKey);
    }
}

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public const string CacheKey = "categories:all";

    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public GetCategoriesHandler(ICategoryRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper     = mapper;
        _cache      = cache;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out IEnumerable<CategoryDto>? cached))
            return cached!;

        var categories = await _repository.GetAllAsync();
        var result = _mapper.Map<IEnumerable<CategoryDto>>(categories);

        _cache.Set(CacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(ICategoryRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        return _mapper.Map<CategoryDto>(category);
    }
}

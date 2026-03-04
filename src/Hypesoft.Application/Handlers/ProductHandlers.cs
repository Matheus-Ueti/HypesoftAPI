using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Hypesoft.Application.Commands;
using Hypesoft.Application.DTOs;
using Hypesoft.Application.Queries;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Exceptions;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Application.Handlers;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public CreateProductHandler(IProductRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Description, request.Price, request.CategoryId, request.Stock);
        await _repository.CreateAsync(product);
        ProductCacheHelper.InvalidateAll(_cache);
        return _mapper.Map<ProductDto>(product);
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public UpdateProductHandler(IProductRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.Update(request.Name, request.Description, request.Price, request.CategoryId, request.Stock);
        await _repository.UpdateAsync(product);
        ProductCacheHelper.InvalidateAll(_cache);
        return _mapper.Map<ProductDto>(product);
    }
}

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;

    public DeleteProductHandler(IProductRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _ = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        await _repository.DeleteAsync(request.Id);
        ProductCacheHelper.InvalidateAll(_cache);
    }
}

public static class ProductCacheHelper
{
    private const string TokenKey = "products_cache_cts";

    public static void InvalidateAll(IMemoryCache cache)
    {
        if (cache.TryGetValue(TokenKey, out CancellationTokenSource? cts))
            cts?.Cancel();
        cache.Remove(TokenKey);
    }

    public static CancellationTokenSource GetOrCreateToken(IMemoryCache cache)
    {
        return cache.GetOrCreate(TokenKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return new CancellationTokenSource();
        })!;
    }
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public GetProductsHandler(IProductRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper     = mapper;
        _cache      = cache;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:{request.Name}:{request.CategoryId}:{request.Page}:{request.PageSize}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<ProductDto>? cached))
            return cached!;

        var (items, total) = await _repository.SearchAsync(request.Name, request.CategoryId, request.Page, request.PageSize);
        var result = new PagedResult<ProductDto>(_mapper.Map<IEnumerable<ProductDto>>(items), total, request.Page, request.PageSize);

        var cts = ProductCacheHelper.GetOrCreateToken(_cache);
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(2))
            .AddExpirationToken(new CancellationChangeToken(cts.Token));
        _cache.Set(cacheKey, result, options);
        return result;
    }
}

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public GetProductByIdHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        return _mapper.Map<ProductDto>(product);
    }
}

public class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public GetLowStockProductsHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repository.GetLowStockAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}

public class UpdateStockHandler : IRequestHandler<UpdateStockCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public UpdateStockHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.UpdateStock(request.Stock);
        await _repository.UpdateAsync(product);
        return _mapper.Map<ProductDto>(product);
    }
}

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public GetDashboardHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // Uma única query para derivar todas as métricas do dashboard
        var all = (await _repository.GetAllAsync()).ToList();

        return new DashboardDto(
            TotalProducts:      all.Count,
            TotalStockValue:    all.Sum(p => p.Price * p.Stock),
            LowStockProducts:   _mapper.Map<IEnumerable<ProductDto>>(all.Where(p => p.Stock < 10)),
            ProductsByCategory: all.GroupBy(p => p.CategoryId)
                                   .Select(g => new CategoryStockDto(g.Key, g.Count()))
        );
    }
}

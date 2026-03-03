using Microsoft.Extensions.DependencyInjection;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();

        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var productRepo  = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var existingProducts = await productRepo.GetAllAsync();
        if (existingProducts.Any()) return;

        var existingCategories = (await categoryRepo.GetAllAsync()).ToList();

        Category eletronicos, vestuario, alimentos;

        if (existingCategories.Any())
        {
            eletronicos = existingCategories.First(c => c.Name == "Eletrônicos");
            vestuario   = existingCategories.First(c => c.Name == "Vestuário");
            alimentos   = existingCategories.First(c => c.Name == "Alimentos");
        }
        else
        {
            eletronicos = Category.Create("Eletrônicos",  "Smartphones, notebooks e gadgets");
            vestuario   = Category.Create("Vestuário",    "Camisetas, calças e acessórios");
            alimentos   = Category.Create("Alimentos",    "Produtos alimentícios e bebidas");

            await categoryRepo.CreateAsync(eletronicos);
            await categoryRepo.CreateAsync(vestuario);
            await categoryRepo.CreateAsync(alimentos);
        }

        var products = new[]
        {
            Product.Create("Notebook Gamer",      "Intel i7, 16GB RAM, RTX 3060", 4999.99m, eletronicos.Id, 15),
            Product.Create("Smartphone Pro",      "128GB, Câmera 108MP",          2499.99m, eletronicos.Id,  8),
            Product.Create("Fone Bluetooth",      "Cancelamento de ruído ativo",   399.99m, eletronicos.Id,  3),
            Product.Create("Camiseta Premium",    "100% algodão, tamanho M",        89.90m, vestuario.Id,   50),
            Product.Create("Tênis Casual",        "Solado antiderrapante",         249.90m, vestuario.Id,   20),
            Product.Create("Café Especial 500g",  "Grão arábica, torra média",      49.90m, alimentos.Id,    6),
            Product.Create("Whey Protein 1kg",    "Sabor baunilha, 30g proteína",  149.90m, alimentos.Id,    2),
        };

        foreach (var product in products)
            await productRepo.CreateAsync(product);
    }
}

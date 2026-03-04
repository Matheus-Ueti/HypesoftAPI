using System.Text.Json;
using System.Threading.RateLimiting;
using Serilog;
using Hypesoft.Application;
using Hypesoft.Infrastructure;
using Hypesoft.API.Extensions;
using Hypesoft.API.Middlewares;
using Hypesoft.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

// Camadas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + camelCase
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// Auth + Swagger
builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddSwaggerWithAuth();
builder.Services.AddEndpointsApiExplorer();

// Cache in-memory
builder.Services.AddMemoryCache();

// Rate limiting — 60 requisições por minuto por IP
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Não aplica rate limiting em OPTIONS (preflight CORS)
        if (context.Request.Method == "OPTIONS")
            return RateLimitPartition.GetNoLimiter("cors-preflight");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 300,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// CORS para o frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Seed de dados
try { await app.Services.SeedAsync(); }
catch (Exception ex) { Log.Warning(ex, "Seed skipped — MongoDB unavailable"); }

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();        // CORS antes do rate limiter — preflight OPTIONS precisa receber os headers corretos
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Pré-carrega as chaves OIDC do Keycloak para evitar 401 nas primeiras requisições
try
{
    var opts = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>()
                           .Get(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
    if (opts.ConfigurationManager != null)
        await opts.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
    Log.Information("OIDC metadata pré-carregado com sucesso");
}
catch (Exception ex) { Log.Warning(ex, "OIDC warmup skipped — Keycloak unavailable"); }

app.Run();

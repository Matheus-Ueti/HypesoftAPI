using System.Text.Json;
using Serilog;
using Hypesoft.Application;
using Hypesoft.Infrastructure;
using Hypesoft.API.Extensions;
using Hypesoft.API.Middlewares;

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

// CORS para o frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hypesoft.Infrastructure.Data;

namespace Hypesoft.API.HealthChecks;

public class MongoHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoHealthCheck(MongoDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var alive = await _context.PingAsync(cancellationToken);
            return alive
                ? HealthCheckResult.Healthy("MongoDB is reachable.")
                : HealthCheckResult.Unhealthy("MongoDB ping returned unexpected result.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unreachable.", ex);
        }
    }
}

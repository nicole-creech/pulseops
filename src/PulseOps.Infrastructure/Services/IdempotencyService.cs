using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Infrastructure.Services;

public class IdempotencyService
{
    private readonly PulseOpsDbContext _dbContext;

    public IdempotencyService(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> GetExistingAsync(
        Guid businessId,
        string endpoint,
        string key,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(
                x => x.BusinessId == businessId &&
                     x.Endpoint == endpoint &&
                     x.Key == key,
                cancellationToken);
    }

    public async Task SaveAsync<T>(
        Guid businessId,
        string endpoint,
        string key,
        T responseObject,
        int statusCode,
        CancellationToken cancellationToken)
    {
        var responseJson = JsonSerializer.Serialize(responseObject);

        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Key = key,
            Endpoint = endpoint,
            ResponseJson = responseJson,
            StatusCode = statusCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.IdempotencyRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
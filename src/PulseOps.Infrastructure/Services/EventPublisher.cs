using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Infrastructure.Services;

public class EventPublisher
{
    private readonly PulseOpsDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public EventPublisher(PulseOpsDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    public async Task PublishAsync(Guid businessId, string eventType, object payload, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload);

        var domainEvent = new DomainEvent
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            EventType = eventType,
            PayloadJson = payloadJson,
            OccurredAtUtc = DateTime.UtcNow,
            Processed = false
        };

        _dbContext.DomainEvents.Add(domainEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var endpoints = await _dbContext.WebhookEndpoints
            .Where(x => x.BusinessId == businessId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var endpoint in endpoints)
        {
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                DomainEventId = domainEvent.Id,
                WebhookEndpointId = endpoint.Id,
                Status = "Pending",
                AttemptCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.WebhookDeliveries.Add(delivery);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                delivery.AttemptCount += 1;
                delivery.LastAttemptAtUtc = DateTime.UtcNow;

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("X-PulseOps-Event-Type", eventType);
                request.Headers.Add("X-PulseOps-Event-Id", domainEvent.Id.ToString());

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                delivery.ResponseStatusCode = (int)response.StatusCode;
                delivery.ResponseBody = responseBody;

                if (response.IsSuccessStatusCode)
                {
                    delivery.Status = "Delivered";
                    delivery.DeliveredAtUtc = DateTime.UtcNow;
                }
                else
                {
                    delivery.Status = "Failed";
                }
            }
            catch (Exception ex)
            {
                delivery.AttemptCount += 1;
                delivery.LastAttemptAtUtc = DateTime.UtcNow;
                delivery.Status = "Failed";
                delivery.ResponseBody = ex.Message;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        domainEvent.Processed = true;
        domainEvent.ProcessedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
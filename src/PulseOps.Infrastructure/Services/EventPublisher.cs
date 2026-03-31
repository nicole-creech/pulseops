using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;
using PulseOps.Infrastructure.Services;

namespace PulseOps.Infrastructure.Services;

public class EventPublisher
{
    private readonly PulseOpsDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly WebhookSignatureService _webhookSignatureService;

    public EventPublisher(PulseOpsDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }
    public EventPublisher(
        PulseOpsDbContext dbContext,
        HttpClient httpClient,
        WebhookSignatureService webhookSignatureService)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _webhookSignatureService = webhookSignatureService;
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
                MaxAttempts = 4,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.WebhookDeliveries.Add(delivery);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var deliveries = await _dbContext.WebhookDeliveries
            .Include(x => x.DomainEvent)
            .Include(x => x.WebhookEndpoint)
            .Where(x => x.DomainEventId == domainEvent.Id)
            .ToListAsync(cancellationToken);

        foreach (var delivery in deliveries)
        {
            await AttemptDeliveryAsync(delivery, cancellationToken);
        }

        domainEvent.Processed = true;
        domainEvent.ProcessedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AttemptDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery.Status == "Delivered" || delivery.Status == "DeadLetter")
        {
            return;
        }

        delivery.AttemptCount += 1;
        delivery.LastAttemptAtUtc = DateTime.UtcNow;
        delivery.LastError = null;

        var signature = _webhookSignatureService.ComputeSignature(
        delivery.WebhookEndpoint.SigningSecret,
        delivery.DomainEvent.PayloadJson);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, delivery.WebhookEndpoint.Url)
            {
                Content = new StringContent(delivery.DomainEvent.PayloadJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-PulseOps-Event-Type", delivery.DomainEvent.EventType);
            request.Headers.Add("X-PulseOps-Event-Id", delivery.DomainEvent.Id.ToString());
            request.Headers.Add("X-PulseOps-Signature", signature);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.ResponseBody = responseBody;

            if (response.IsSuccessStatusCode)
            {
                delivery.Status = "Delivered";
                delivery.DeliveredAtUtc = DateTime.UtcNow;
                delivery.NextRetryAtUtc = null;
            }
            else
            {
                MarkRetryState(delivery, $"Received HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            delivery.ResponseStatusCode = null;
            delivery.ResponseBody = null;
            MarkRetryState(delivery, ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RetryDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await _dbContext.WebhookDeliveries
            .Include(x => x.DomainEvent)
            .Include(x => x.WebhookEndpoint)
            .FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);

        if (delivery is null)
        {
            throw new InvalidOperationException("Webhook delivery not found.");
        }

        await AttemptDeliveryAsync(delivery, cancellationToken);
    }

    public async Task ProcessDueRetriesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var dueDeliveries = await _dbContext.WebhookDeliveries
            .Include(x => x.DomainEvent)
            .Include(x => x.WebhookEndpoint)
            .Where(x =>
                x.Status == "RetryScheduled" &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var delivery in dueDeliveries)
        {
            await AttemptDeliveryAsync(delivery, cancellationToken);
        }
    }

    private static void MarkRetryState(WebhookDelivery delivery, string error)
    {
        delivery.LastError = error;

        if (delivery.AttemptCount >= delivery.MaxAttempts)
        {
            delivery.Status = "DeadLetter";
            delivery.NextRetryAtUtc = null;
            return;
        }

        delivery.Status = "RetryScheduled";
        delivery.NextRetryAtUtc = delivery.AttemptCount switch
        {
            1 => DateTime.UtcNow.AddMinutes(1),
            2 => DateTime.UtcNow.AddMinutes(5),
            3 => DateTime.UtcNow.AddMinutes(15),
            _ => DateTime.UtcNow.AddHours(1)
        };
    }
}
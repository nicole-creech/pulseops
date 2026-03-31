using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;
using PulseOps.Infrastructure.Services;
using PulseOps.Infrastructure.Services;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;
    private readonly EventPublisher _eventPublisher;
    private readonly WebhookSignatureService _webhookSignatureService;


    public WebhooksController(PulseOpsDbContext dbContext, EventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    public WebhooksController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public WebhooksController(
        PulseOpsDbContext dbContext,
        EventPublisher eventPublisher,
        WebhookSignatureService webhookSignatureService)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _webhookSignatureService = webhookSignatureService;
    }

    [HttpGet("endpoints")]
    public async Task<ActionResult<IEnumerable<WebhookEndpointResponse>>> GetEndpoints(CancellationToken cancellationToken)
    {
        var endpoints = await _dbContext.WebhookEndpoints
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WebhookEndpointResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                Name = x.Name,
                Url = x.Url,
                SigningSecretPreview = x.SigningSecret.Substring(0, 8) + "...",
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(endpoints);
    }

    [HttpPost("endpoints")]
    public async Task<ActionResult<WebhookEndpointResponse>> CreateEndpoint(
        [FromBody] CreateWebhookEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var businessExists = await _dbContext.Businesses
            .AnyAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
        {
            return BadRequest("Business does not exist.");
        }

        var signingSecret = _webhookSignatureService.GenerateSecret();

        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            SigningSecret = signingSecret,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.WebhookEndpoints.Add(endpoint);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WebhookEndpointResponse
        {
            Id = endpoint.Id,
            BusinessId = endpoint.BusinessId,
            Name = endpoint.Name,
            Url = endpoint.Url,
            SigningSecretPreview = signingSecret[..8] + "...",
            IsActive = endpoint.IsActive,
            CreatedAtUtc = endpoint.CreatedAtUtc
        };

        return Ok(response);
    }

   [HttpGet("deliveries")]
    public async Task<ActionResult<IEnumerable<WebhookDeliveryResponse>>> GetDeliveries(CancellationToken cancellationToken)
    {
        var deliveries = await _dbContext.WebhookDeliveries
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WebhookDeliveryResponse
            {
                Id = x.Id,
                DomainEventId = x.DomainEventId,
                WebhookEndpointId = x.WebhookEndpointId,
                Status = x.Status,
                AttemptCount = x.AttemptCount,
                MaxAttempts = x.MaxAttempts,
                ResponseStatusCode = x.ResponseStatusCode,
                ResponseBody = x.ResponseBody,
                LastError = x.LastError,
                CreatedAtUtc = x.CreatedAtUtc,
                LastAttemptAtUtc = x.LastAttemptAtUtc,
                NextRetryAtUtc = x.NextRetryAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(deliveries);
    }

    [HttpPost("deliveries/{id:guid}/retry")]
    public async Task<IActionResult> RetryDelivery(Guid id, CancellationToken cancellationToken)
    {
        var deliveryExists = await _dbContext.WebhookDeliveries
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!deliveryExists)
        {
            return NotFound("Webhook delivery not found.");
        }

        await _eventPublisher.RetryDeliveryAsync(id, cancellationToken);

        return Ok(new
        {
            deliveryId = id,
            message = "Retry attempted."
        });
    }
    [HttpPost("deliveries/process-due-retries")]
    public async Task<IActionResult> ProcessDueRetries(CancellationToken cancellationToken)
    {
        await _eventPublisher.ProcessDueRetriesAsync(cancellationToken);

        return Ok(new
        {
            message = "Processed due retries."
        });
    }
}
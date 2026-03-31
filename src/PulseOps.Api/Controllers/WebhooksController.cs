using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public WebhooksController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
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
                ResponseStatusCode = x.ResponseStatusCode,
                ResponseBody = x.ResponseBody,
                CreatedAtUtc = x.CreatedAtUtc,
                LastAttemptAtUtc = x.LastAttemptAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(deliveries);
    }
}
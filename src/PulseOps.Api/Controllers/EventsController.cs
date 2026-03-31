using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public EventsController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DomainEventResponse>>> GetEvents(CancellationToken cancellationToken)
    {
        var events = await _dbContext.DomainEvents
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new DomainEventResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                EventType = x.EventType,
                PayloadJson = x.PayloadJson,
                OccurredAtUtc = x.OccurredAtUtc,
                Processed = x.Processed,
                ProcessedAtUtc = x.ProcessedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}
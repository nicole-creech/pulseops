using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;
using PulseOps.Infrastructure.Services;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public CustomersController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private readonly IdempotencyService _idempotencyService;

    public CustomersController(
        PulseOpsDbContext dbContext,
        IdempotencyService idempotencyService)
    {
        _dbContext = dbContext;
        _idempotencyService = idempotencyService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _dbContext.Customers
            .OrderBy(x => x.FullName)
            .Select(x => new CustomerResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var businessExists = await _dbContext.Businesses
            .AnyAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
        {
            return BadRequest("Business does not exist.");
        }

        string? idempotencyKey = null;

        if (Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            idempotencyKey = keyValues.FirstOrDefault()?.Trim();
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyService.GetExistingAsync(
                request.BusinessId,
                "POST:/api/customers",
                idempotencyKey,
                cancellationToken);

            if (existing is not null)
            {
                return Content(existing.ResponseJson, "application/json");
            }
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new CustomerResponse
        {
            Id = customer.Id,
            BusinessId = customer.BusinessId,
            FullName = customer.FullName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            CreatedAtUtc = customer.CreatedAtUtc
        };

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _idempotencyService.SaveAsync(
                request.BusinessId,
                "POST:/api/customers",
                idempotencyKey,
                response,
                StatusCodes.Status200OK,
                cancellationToken);
        }

        return Ok(response);
    }
}
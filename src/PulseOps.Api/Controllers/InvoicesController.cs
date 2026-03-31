using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public InvoicesController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceResponse>>> GetInvoices(CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.Invoices
            .OrderByDescending(x => x.IssuedAtUtc)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                OrderId = x.OrderId,
                InvoiceNumber = x.InvoiceNumber,
                Amount = x.Amount,
                Status = x.Status,
                IssuedAtUtc = x.IssuedAtUtc,
                PaidAtUtc = x.PaidAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Invoices
            .Where(x => x.Id == id)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                OrderId = x.OrderId,
                InvoiceNumber = x.InvoiceNumber,
                Amount = x.Amount,
                Status = x.Status,
                IssuedAtUtc = x.IssuedAtUtc,
                PaidAtUtc = x.PaidAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpPatch("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkInvoicePaid(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound("Invoice not found.");
        }

        if (invoice.Status == "Paid")
        {
            return BadRequest("Invoice is already marked as paid.");
        }

        invoice.Status = "Paid";
        invoice.PaidAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            invoiceId = invoice.Id,
            status = invoice.Status,
            paidAtUtc = invoice.PaidAtUtc
        });
    }
}
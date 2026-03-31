using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public OrdersController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one order item is required.");
        }

        var businessExists = await _dbContext.Businesses
            .AnyAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
        {
            return BadRequest("Business does not exist.");
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(
                x => x.Id == request.CustomerId && x.BusinessId == request.BusinessId,
                cancellationToken);

        if (customer is null)
        {
            return BadRequest("Customer does not exist for this business.");
        }

        var requestedProductIds = request.Items.Select(x => x.ProductId).Distinct().ToList();

        var products = await _dbContext.Products
            .Include(x => x.InventoryItem)
            .Where(x => requestedProductIds.Contains(x.Id) && x.BusinessId == request.BusinessId)
            .ToListAsync(cancellationToken);

        if (products.Count != requestedProductIds.Count)
        {
            return BadRequest("One or more products were not found for this business.");
        }

        foreach (var item in request.Items)
        {
            var product = products.First(x => x.Id == item.ProductId);

            if (product.InventoryItem is null)
            {
                return BadRequest($"Inventory record missing for product {product.Name}.");
            }

            var availableQuantity = product.InventoryItem.QuantityOnHand - product.InventoryItem.ReservedQuantity;

            if (item.Quantity <= 0)
            {
                return BadRequest("Order item quantities must be greater than zero.");
            }

            if (availableQuantity < item.Quantity)
            {
                return BadRequest($"Not enough inventory available for product {product.Name}.");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            CustomerId = request.CustomerId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        decimal totalAmount = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var inventory = product.InventoryItem!;

            inventory.ReservedQuantity += item.Quantity;
            inventory.UpdatedAtUtc = DateTime.UtcNow;

            var lineTotal = product.Price * item.Quantity;
            totalAmount += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });
        }

        order.TotalAmount = totalAmount;
        order.Items = orderItems;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new OrderResponse
        {
            Id = order.Id,
            BusinessId = order.BusinessId,
            CustomerId = order.CustomerId,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = orderItems.Select(x => new OrderItemResponse
            {
                ProductId = x.ProductId,
                ProductName = products.First(p => p.Id == x.ProductId).Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> CompleteOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound("Order not found.");
        }

        if (order.Status == "Completed")
        {
            return BadRequest("Order is already completed.");
        }

        foreach (var item in order.Items)
        {
            var inventory = item.Product.InventoryItem;

            if (inventory is null)
            {
                return BadRequest($"Inventory missing for product {item.Product.Name}");
            }

            if (inventory.ReservedQuantity < item.Quantity)
            {
                return BadRequest($"Invalid reservation state for product {item.Product.Name}");
            }

            inventory.ReservedQuantity -= item.Quantity;
            inventory.QuantityOnHand -= item.Quantity;
            inventory.UpdatedAtUtc = DateTime.UtcNow;
        }

        order.Status = "Completed";

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            orderId = order.Id,
            status = order.Status,
            message = "Order completed and inventory fulfilled"
        });
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _orderService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var order = await _orderService.GetByIdAsync(id, TenantId);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order)
    {
        order.TenantId = TenantId;
        var result = await _orderService.CreateAsync(order);
        return result.Success ? Created($"/api/orders/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(string id)
    {
        var result = await _orderService.ConfirmAsync(id, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelOrderRequest request)
    {
        var result = await _orderService.CancelAsync(id, request.Reason, TenantId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}

public class CancelOrderRequest { public string Reason { get; set; } = string.Empty; }

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _customerService.GetAllAsync(TenantId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var customer = await _customerService.GetByIdAsync(id, TenantId);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        customer.TenantId = TenantId;
        var result = await _customerService.CreateAsync(customer);
        return result.Success ? Created($"/api/customers/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Customer customer)
    {
        customer.Id = id;
        customer.TenantId = TenantId;
        var result = await _customerService.UpdateAsync(customer);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _customerService.DeleteAsync(id, TenantId);
        return deleted ? NoContent() : NotFound();
    }
}

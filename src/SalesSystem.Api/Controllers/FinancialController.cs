using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinancialController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public FinancialController(IFinancialService financialService) => _financialService = financialService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (from.HasValue && to.HasValue)
        {
            var toDate = to.Value;
            if (toDate.Hour == 0 && toDate.Minute == 0 && toDate.Second == 0)
                toDate = toDate.Date.AddDays(1).AddTicks(-1);
            return Ok(await _financialService.GetByPeriodAsync(TenantId, from.Value, toDate));
        }
        return Ok(await _financialService.GetAllAsync(TenantId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entry = await _financialService.GetByIdAsync(id, TenantId);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FinancialEntry entry)
    {
        entry.TenantId = TenantId;
        var result = await _financialService.CreateAsync(entry);
        return result.Success ? Created($"/api/financial/{result.Data!.Id}", result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] FinancialEntry entry)
    {
        entry.Id = id;
        entry.TenantId = TenantId;
        var result = await _financialService.UpdateAsync(entry);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        // Ensure 'to' covers the entire day
        if (to.Hour == 0 && to.Minute == 0 && to.Second == 0)
            to = to.Date.AddDays(1).AddTicks(-1);
        return Ok(await _financialService.GetSummaryAsync(TenantId, from, to));
    }
}

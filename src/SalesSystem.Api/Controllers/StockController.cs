using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService) => _stockService = stockService;

    private string TenantId => HttpContext.Items["TenantId"]?.ToString() ?? string.Empty;

    [HttpGet("balances")]
    public async Task<IActionResult> GetAllBalances()
        => Ok(await _stockService.GetAllBalancesAsync(TenantId));

    [HttpGet("balances/{productId}")]
    public async Task<IActionResult> GetBalance(string productId)
    {
        var balance = await _stockService.GetBalanceByProductAsync(productId, TenantId);
        return balance is null ? NotFound() : Ok(balance);
    }

    [HttpGet("moves/{productId}")]
    public async Task<IActionResult> GetMoves(string productId)
        => Ok(await _stockService.GetMovesAsync(productId, TenantId));

    [HttpPost("moves")]
    public async Task<IActionResult> AddMove([FromBody] StockMove move)
    {
        move.TenantId = TenantId;
        var result = await _stockService.AddMoveAsync(move);
        return result.Success ? Created("", result.Data) : BadRequest(new { error = result.Error });
    }
}

using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(string id, string tenantId);
    Task<List<Order>> GetAllAsync(string tenantId);
    Task<ServiceResult<Order>> CreateAsync(Order order);
    Task<ServiceResult<Order>> ConfirmAsync(string id, string tenantId);
    Task<ServiceResult<Order>> CancelAsync(string id, string reason, string tenantId);
}

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly IStockService _stockService;
    private readonly IFinancialService _financialService;

    public OrderService(IOrderRepository repo, IStockService stockService, IFinancialService financialService)
    {
        _repo = repo;
        _stockService = stockService;
        _financialService = financialService;
    }

    public async Task<Order?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<List<Order>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<Order>> CreateAsync(Order order)
    {
        if (order.Items.Count == 0)
            return ServiceResult<Order>.Fail("Order must have at least one item.");

        // Calculate item totals
        foreach (var item in order.Items)
        {
            item.DiscountAmount = item.UnitPrice * item.Quantity * (item.DiscountPct / 100m);
            item.TotalPrice = (item.UnitPrice * item.Quantity) - item.DiscountAmount;
            item.TaxAmount = item.TotalPrice * (item.TaxRate / 100m);
            item.TotalCost = item.UnitCost * item.Quantity;
        }

        // Calculate order totals
        order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.DiscountTotal = order.Items.Sum(i => i.DiscountAmount);
        order.TaxTotal = order.Items.Sum(i => i.TaxAmount);
        order.Total = order.Subtotal - order.DiscountTotal + order.TaxTotal + order.ShippingCost;
        order.TotalCost = order.Items.Sum(i => i.TotalCost);
        order.GrossMargin = order.Total - order.TotalCost;
        order.GrossMarginPct = order.Total > 0 ? (order.GrossMargin / order.Total) * 100m : 0;

        order.AmountPaid = order.Payments.Where(p => p.Status == PaymentStatus.Pago).Sum(p => p.Amount);
        order.AmountDue = order.Total - order.AmountPaid;

        // Generate order number if not set
        if (string.IsNullOrEmpty(order.OrderNumber))
        {
            var count = await _repo.CountByStatusAsync(OrderStatus.Rascunho, order.TenantId)
                      + await _repo.CountByStatusAsync(OrderStatus.Confirmado, order.TenantId)
                      + await _repo.CountByStatusAsync(OrderStatus.Faturado, order.TenantId)
                      + await _repo.CountByStatusAsync(OrderStatus.Cancelado, order.TenantId)
                      + await _repo.CountByStatusAsync(OrderStatus.Devolvido, order.TenantId);

            order.OrderNumber = $"PED-{(count + 1):D6}";
        }

        order.Status = OrderStatus.Rascunho;

        var created = await _repo.InsertAsync(order);
        return ServiceResult<Order>.Ok(created);
    }

    public async Task<ServiceResult<Order>> ConfirmAsync(string id, string tenantId)
    {
        var order = await _repo.GetByIdAsync(id, tenantId);
        if (order is null)
            return ServiceResult<Order>.Fail("Order not found.");

        if (order.Status != OrderStatus.Rascunho)
            return ServiceResult<Order>.Fail($"Only draft orders can be confirmed. Current status: {order.Status}.");

        // VALIDATION PASS - check all items have sufficient stock before consuming any
        foreach (var item in order.Items)
        {
            var balance = await _stockService.GetBalanceByProductAsync(item.ProductId, tenantId);
            var available = balance?.AvailableBalance ?? 0;
            if (available < item.Quantity)
                return ServiceResult<Order>.Fail($"Estoque insuficiente de '{item.Name}'. Necessario: {item.Quantity}, Disponivel: {available}");
        }

        // CONSUMPTION PASS - only runs if all validations passed
        foreach (var item in order.Items)
        {
            var stockMove = new StockMove
            {
                TenantId = tenantId,
                ProductId = item.ProductId,
                ProductName = item.Name,
                ProductSku = item.Sku,
                Type = StockMoveType.Saida,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                ReferenceId = order.Id,
                ReferenceType = "Order",
                Note = $"Order {order.OrderNumber} confirmed"
            };

            var stockResult = await _stockService.AddMoveAsync(stockMove);
            if (!stockResult.Success)
                return ServiceResult<Order>.Fail($"Stock error for product '{item.Name}': {stockResult.Error}");
        }

        // Create financial entries
        if (order.Payments.Count > 0)
        {
            foreach (var payment in order.Payments)
            {
                var entry = new FinancialEntry
                {
                    TenantId = tenantId,
                    Type = FinancialType.Receita,
                    Category = FinancialCategory.Venda,
                    Description = $"Pedido {order.OrderNumber} - {payment.Method}",
                    Amount = payment.Amount,
                    AmountPaid = payment.Status == PaymentStatus.Pago ? payment.Amount : 0,
                    DueDate = payment.DueDate,
                    PaymentDate = payment.PaidAt,
                    Status = payment.Status == PaymentStatus.Pago ? FinancialStatus.Pago : FinancialStatus.Pendente,
                    ReferenceId = order.Id,
                    ReferenceType = "Order",
                    PaymentMethod = payment.Method.ToString(),
                    CustomerId = order.CustomerId,
                    CustomerName = order.CustomerName,
                    InstallmentIndex = payment.Installments,
                    InstallmentTotal = order.Payments.Count
                };
                await _financialService.CreateAsync(entry);
            }
        }
        else
        {
            // No payments defined — create a single financial entry for the full order total
            var entry = new FinancialEntry
            {
                TenantId = tenantId,
                Type = FinancialType.Receita,
                Category = FinancialCategory.Venda,
                Description = $"Venda - Pedido {order.OrderNumber} - {order.CustomerName}",
                Amount = order.Total,
                AmountPaid = order.Total,
                AmountDue = 0,
                DueDate = DateTime.UtcNow,
                PaymentDate = DateTime.UtcNow,
                Status = FinancialStatus.Pago,
                ReferenceId = order.Id,
                ReferenceType = "Order",
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName
            };
            await _financialService.CreateAsync(entry);
        }

        order.Status = OrderStatus.Confirmado;
        order.ConfirmedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order);
        return ServiceResult<Order>.Ok(order);
    }

    public async Task<ServiceResult<Order>> CancelAsync(string id, string reason, string tenantId)
    {
        var order = await _repo.GetByIdAsync(id, tenantId);
        if (order is null)
            return ServiceResult<Order>.Fail("Order not found.");

        if (order.Status == OrderStatus.Cancelado)
            return ServiceResult<Order>.Fail("Order is already cancelled.");

        if (order.Status == OrderStatus.Faturado)
            return ServiceResult<Order>.Fail("Cannot cancel an invoiced order.");

        // If the order was confirmed, reverse stock moves
        if (order.Status == OrderStatus.Confirmado)
        {
            foreach (var item in order.Items)
            {
                var stockMove = new StockMove
                {
                    TenantId = tenantId,
                    ProductId = item.ProductId,
                    ProductName = item.Name,
                    ProductSku = item.Sku,
                    Type = StockMoveType.Devolucao,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    ReferenceId = order.Id,
                    ReferenceType = "OrderCancellation",
                    Note = $"Order {order.OrderNumber} cancelled: {reason}"
                };

                await _stockService.AddMoveAsync(stockMove);
            }
        }

        order.Status = OrderStatus.Cancelado;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = reason;

        await _repo.UpdateAsync(order);
        return ServiceResult<Order>.Ok(order);
    }
}

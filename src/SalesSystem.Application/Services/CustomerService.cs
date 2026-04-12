using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(string id, string tenantId);
    Task<List<Customer>> GetAllAsync(string tenantId);
    Task<ServiceResult<Customer>> CreateAsync(Customer customer);
    Task<ServiceResult<Customer>> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(string id, string tenantId);
}

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public CustomerType Type { get; set; }
    public bool IsActive { get; set; }
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo)
    {
        _repo = repo;
    }

    public async Task<Customer?> GetByIdAsync(string id, string tenantId)
        => await _repo.GetByIdAsync(id, tenantId);

    public async Task<List<Customer>> GetAllAsync(string tenantId)
        => await _repo.GetAllAsync(tenantId);

    public async Task<ServiceResult<Customer>> CreateAsync(Customer customer)
    {
        var existing = await _repo.GetByDocumentAsync(customer.Document, customer.TenantId);
        if (existing is not null)
            return ServiceResult<Customer>.Fail($"A customer with document '{customer.Document}' already exists.");

        var all = await _repo.GetAllAsync(customer.TenantId);
        var maxCode = all.Select(c => { var num = c.Code?.Replace("CLI-",""); return int.TryParse(num, out var n) ? n : 0; }).DefaultIfEmpty(0).Max();
        customer.Code = $"CLI-{(maxCode + 1):D5}";

        var created = await _repo.InsertAsync(customer);
        return ServiceResult<Customer>.Ok(created);
    }

    public async Task<ServiceResult<Customer>> UpdateAsync(Customer customer)
    {
        var existing = await _repo.GetByIdAsync(customer.Id, customer.TenantId);
        if (existing is null)
            return ServiceResult<Customer>.Fail("Customer not found.");

        // If document changed, check for duplicates
        if (existing.Document != customer.Document)
        {
            var duplicate = await _repo.GetByDocumentAsync(customer.Document, customer.TenantId);
            if (duplicate is not null)
                return ServiceResult<Customer>.Fail($"A customer with document '{customer.Document}' already exists.");
        }

        await _repo.UpdateAsync(customer);
        return ServiceResult<Customer>.Ok(customer);
    }

    public async Task<bool> DeleteAsync(string id, string tenantId)
        => await _repo.DeleteAsync(id, tenantId);
}

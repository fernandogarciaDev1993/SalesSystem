using FluentAssertions;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Services;
using SalesSystem.Application.Vocabularies;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Tests.Services;

public class TenantServiceVocabularyTests
{
    private readonly Mock<ITenantRepository> _repo = new();
    private readonly TenantService _sut;

    private const string TenantId = "tenant-vocab-1";

    public TenantServiceVocabularyTests()
    {
        _sut = new TenantService(_repo.Object);
    }

    [Fact]
    public async Task UpdateVocabularyAsync_TenantNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(TenantId)).ReturnsAsync((Tenant?)null);

        var act = async () => await _sut.UpdateVocabularyAsync(TenantId, new TenantVocabulary());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVocabularyAsync_PersistsVocabularyAndTimestamp()
    {
        var tenant = new Tenant
        {
            Id = TenantId,
            Name = "Test",
            Subdomain = "test",
            UpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        _repo.Setup(r => r.GetByIdAsync(TenantId)).ReturnsAsync(tenant);
        Tenant? savedTenant = null;
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>()))
            .Callback<Tenant>(t => savedTenant = t)
            .Returns(Task.CompletedTask);

        var newVocab = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Restaurante,
            Overrides = new Dictionary<string, VocabularyTermDoc>
            {
                [LabelKeys.Recipe] = new() { Singular = "Fórmula", Plural = "Fórmulas" }
            }
        };
        var before = DateTime.UtcNow;

        await _sut.UpdateVocabularyAsync(TenantId, newVocab);

        _repo.Verify(r => r.UpdateAsync(It.IsAny<Tenant>()), Times.Once);
        savedTenant.Should().NotBeNull();
        savedTenant!.Vocabulary.Should().BeSameAs(newVocab);
        savedTenant.Vocabulary.PresetId.Should().Be(VocabularyPresetIds.Restaurante);
        savedTenant.Vocabulary.Overrides.Should().ContainKey(LabelKeys.Recipe);
        savedTenant.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task UpdateVocabularyAsync_ReplacesPreviousVocabulary()
    {
        var tenant = new Tenant
        {
            Id = TenantId,
            Vocabulary = new TenantVocabulary
            {
                PresetId = VocabularyPresetIds.Confeitaria,
                Overrides = new Dictionary<string, VocabularyTermDoc>
                {
                    [LabelKeys.Product] = new() { Singular = "Old", Plural = "Olds" }
                }
            }
        };
        _repo.Setup(r => r.GetByIdAsync(TenantId)).ReturnsAsync(tenant);
        Tenant? savedTenant = null;
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>()))
            .Callback<Tenant>(t => savedTenant = t)
            .Returns(Task.CompletedTask);

        var replacement = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Grafica,
            Overrides = [],
        };

        await _sut.UpdateVocabularyAsync(TenantId, replacement);

        savedTenant!.Vocabulary.PresetId.Should().Be(VocabularyPresetIds.Grafica);
        savedTenant.Vocabulary.Overrides.Should().BeEmpty(
            "previous overrides must be cleared when vocabulary is replaced");
    }

    [Fact]
    public async Task UpdateVocabularyAsync_ThroughResolver_ReflectsChanges()
    {
        // Integration-ish: after UpdateVocabularyAsync, resolving the tenant's vocabulary
        // should produce terms consistent with what was saved.
        var tenant = new Tenant { Id = TenantId };
        _repo.Setup(r => r.GetByIdAsync(TenantId)).ReturnsAsync(tenant);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>())).Returns(Task.CompletedTask);

        var vocab = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Restaurante,
            Overrides = new Dictionary<string, VocabularyTermDoc>
            {
                [LabelKeys.Order] = new()
                {
                    Singular           = "Ticket",
                    Plural             = "Tickets",
                    ArticleSingular    = "o",
                    ArticlePlural      = "os",
                    IndefiniteSingular = "um",
                }
            }
        };

        await _sut.UpdateVocabularyAsync(TenantId, vocab);

        var resolved = VocabularyResolver.Resolve(tenant.Vocabulary);
        resolved.Terms[LabelKeys.Order].Singular.Should().Be("Ticket");        // from override
        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Ficha Técnica"); // from Restaurante preset
    }
}

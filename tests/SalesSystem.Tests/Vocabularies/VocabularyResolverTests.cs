using FluentAssertions;
using SalesSystem.Application.Services;
using SalesSystem.Application.Vocabularies;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Tests.Vocabularies;

public class VocabularyResolverTests
{
    [Fact]
    public void Resolve_Null_UsesDefaultPreset()
    {
        var resolved = VocabularyResolver.Resolve(null);

        resolved.PresetId.Should().Be(VocabularyPresets.DefaultPresetId);
        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Receita");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyPresetId_UsesDefault(string presetId)
    {
        var vocab = new TenantVocabulary { PresetId = presetId };

        var resolved = VocabularyResolver.Resolve(vocab);

        resolved.PresetId.Should().Be(VocabularyPresets.DefaultPresetId);
        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Receita");
    }

    [Fact]
    public void Resolve_UnknownPresetId_FallsBackToDefaultTerms_ButKeepsPresetIdField()
    {
        var vocab = new TenantVocabulary { PresetId = "does-not-exist" };

        var resolved = VocabularyResolver.Resolve(vocab);

        // PresetId field is preserved (so the admin UI can still show what was stored),
        // but the TERMS come from the default preset.
        resolved.PresetId.Should().Be("does-not-exist");
        resolved.Terms[LabelKeys.Recipe].Singular
            .Should().Be(VocabularyPresets.Presets[VocabularyPresets.DefaultPresetId][LabelKeys.Recipe].Singular);
    }

    [Fact]
    public void Resolve_KnownPresetNoOverrides_ReturnsPresetTerms()
    {
        var vocab = new TenantVocabulary { PresetId = VocabularyPresetIds.Restaurante };

        var resolved = VocabularyResolver.Resolve(vocab);

        resolved.PresetId.Should().Be(VocabularyPresetIds.Restaurante);
        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Ficha Técnica");
        resolved.Terms[LabelKeys.Product].Singular.Should().Be("Prato");
        resolved.Terms[LabelKeys.Order].Singular.Should().Be("Comanda");
    }

    [Fact]
    public void Resolve_OverrideTakesPrecedenceOverPreset()
    {
        var vocab = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Confeitaria,
            Overrides = new Dictionary<string, VocabularyTermDoc>
            {
                [LabelKeys.Recipe] = new()
                {
                    Singular           = "Fórmula",
                    Plural             = "Fórmulas",
                    ArticleSingular    = "a",
                    ArticlePlural      = "as",
                    IndefiniteSingular = "uma",
                }
            }
        };

        var resolved = VocabularyResolver.Resolve(vocab);

        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Fórmula");
        resolved.Terms[LabelKeys.Recipe].Plural.Should().Be("Fórmulas");
    }

    [Fact]
    public void Resolve_NonOverriddenKeys_StillComeFromPreset()
    {
        var vocab = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Restaurante,
            Overrides = new Dictionary<string, VocabularyTermDoc>
            {
                // Override only Recipe; Product and Order should remain from Restaurante preset.
                [LabelKeys.Recipe] = new()
                {
                    Singular           = "Cardápio",
                    Plural             = "Cardápios",
                    ArticleSingular    = "o",
                    ArticlePlural      = "os",
                    IndefiniteSingular = "um",
                }
            }
        };

        var resolved = VocabularyResolver.Resolve(vocab);

        resolved.Terms[LabelKeys.Recipe].Singular.Should().Be("Cardápio");
        resolved.Terms[LabelKeys.Product].Singular.Should().Be("Prato");   // from Restaurante
        resolved.Terms[LabelKeys.Order].Singular.Should().Be("Comanda");   // from Restaurante
    }

    [Fact]
    public void Resolve_AlwaysPopulatesAllLabelKeys()
    {
        var vocab = new TenantVocabulary { PresetId = VocabularyPresetIds.Grafica };

        var resolved = VocabularyResolver.Resolve(vocab);

        foreach (var key in LabelKeys.All)
        {
            resolved.Terms.Should().ContainKey(key,
                $"resolved vocabulary must always contain every label key ('{key}' missing)");
        }
    }

    [Fact]
    public void Resolve_DoesNotMutateInputVocabulary()
    {
        var originalOverrides = new Dictionary<string, VocabularyTermDoc>
        {
            [LabelKeys.Recipe] = new() { Singular = "X", Plural = "Xs" }
        };
        var vocab = new TenantVocabulary
        {
            PresetId = VocabularyPresetIds.Confeitaria,
            Overrides = originalOverrides,
        };

        _ = VocabularyResolver.Resolve(vocab);

        vocab.PresetId.Should().Be(VocabularyPresetIds.Confeitaria);
        vocab.Overrides.Should().BeSameAs(originalOverrides);
        vocab.Overrides.Should().HaveCount(1);
    }
}

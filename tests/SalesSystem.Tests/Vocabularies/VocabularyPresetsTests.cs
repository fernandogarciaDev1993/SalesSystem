using FluentAssertions;
using SalesSystem.Application.Vocabularies;

namespace SalesSystem.Tests.Vocabularies;

public class VocabularyPresetsTests
{
    [Fact]
    public void DefaultPresetId_IsConfeitaria()
    {
        VocabularyPresets.DefaultPresetId.Should().Be(VocabularyPresetIds.Confeitaria);
    }

    [Theory]
    [InlineData(VocabularyPresetIds.Confeitaria, "Receita")]
    [InlineData(VocabularyPresetIds.Restaurante, "Ficha Técnica")]
    [InlineData(VocabularyPresetIds.Grafica,     "Ficha Técnica")]
    [InlineData(VocabularyPresetIds.Brindes,     "Composição")]
    [InlineData(VocabularyPresetIds.Generico,    "Ficha Técnica")]
    public void GetPresetOrDefault_KnownId_ReturnsExpectedRecipeTerm(string presetId, string expected)
    {
        var preset = VocabularyPresets.GetPresetOrDefault(presetId);

        preset[LabelKeys.Recipe].Singular.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("preset-that-does-not-exist")]
    public void GetPresetOrDefault_NullEmptyOrUnknown_ReturnsDefaultPreset(string? presetId)
    {
        var preset = VocabularyPresets.GetPresetOrDefault(presetId);
        var expectedDefault = VocabularyPresets.Presets[VocabularyPresets.DefaultPresetId];

        preset.Should().BeSameAs(expectedDefault);
    }

    [Fact]
    public void AllPresets_ContainAllLabelKeys()
    {
        foreach (var presetId in VocabularyPresetIds.All)
        {
            var preset = VocabularyPresets.Presets[presetId];
            foreach (var key in LabelKeys.All)
            {
                preset.ContainsKey(key).Should().BeTrue(
                    $"preset '{presetId}' should define term for label key '{key}'");
            }
        }
    }

    [Fact]
    public void AllPresetTerms_HaveNonEmptySingularAndPlural()
    {
        foreach (var (presetId, preset) in VocabularyPresets.Presets)
        {
            foreach (var (key, term) in preset)
            {
                term.Singular.Should().NotBeNullOrWhiteSpace(
                    $"preset '{presetId}' key '{key}' must have a singular form");
                term.Plural.Should().NotBeNullOrWhiteSpace(
                    $"preset '{presetId}' key '{key}' must have a plural form");
            }
        }
    }

    [Theory]
    [InlineData("o", "os", "um")]
    [InlineData("a", "as", "uma")]
    public void AllPresetTerms_UseConsistentArticleGender(string articleSingular, string articlePlural, string indefinite)
    {
        foreach (var (presetId, preset) in VocabularyPresets.Presets)
        {
            foreach (var (key, term) in preset)
            {
                if (term.ArticleSingular != articleSingular) continue;

                term.ArticlePlural.Should().Be(articlePlural,
                    $"{presetId}.{key}: article plural must match gender of article singular");
                term.IndefiniteSingular.Should().Be(indefinite,
                    $"{presetId}.{key}: indefinite singular must match gender of article singular");
            }
        }
    }

    [Fact]
    public void GetDefaultTerm_KnownKey_ReturnsTermFromDefaultPreset()
    {
        var term = VocabularyPresets.GetDefaultTerm(LabelKeys.Recipe);
        var expected = VocabularyPresets.Presets[VocabularyPresets.DefaultPresetId][LabelKeys.Recipe];

        term.Should().BeSameAs(expected);
    }

    [Fact]
    public void GetDefaultTerm_UnknownKey_ReturnsKeyBasedFallback()
    {
        var term = VocabularyPresets.GetDefaultTerm("unknown.key");

        term.Singular.Should().Be("unknown.key");
        term.Plural.Should().Be("unknown.key");
    }

    [Fact]
    public void RestaurantePreset_UsesRestaurantSpecificTerms()
    {
        var preset = VocabularyPresets.Presets[VocabularyPresetIds.Restaurante];

        preset[LabelKeys.Product].Singular.Should().Be("Prato");
        preset[LabelKeys.Order].Singular.Should().Be("Comanda");
    }

    [Fact]
    public void GraficaPreset_UsesPrintShopTerms()
    {
        var preset = VocabularyPresets.Presets[VocabularyPresetIds.Grafica];

        preset[LabelKeys.Insumo].Singular.Should().Be("Matéria-Prima");
        preset[LabelKeys.Order].Singular.Should().Be("Orçamento");
        preset[LabelKeys.Yield].Singular.Should().Be("Tiragem");
    }

    [Fact]
    public void BrindesPreset_UsesGiftCompanyTerms()
    {
        var preset = VocabularyPresets.Presets[VocabularyPresetIds.Brindes];

        preset[LabelKeys.Product].Singular.Should().Be("Kit");
        preset[LabelKeys.Recipe].Singular.Should().Be("Composição");
    }
}

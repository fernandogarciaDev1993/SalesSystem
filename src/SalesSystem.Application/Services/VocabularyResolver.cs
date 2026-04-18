using SalesSystem.Application.Vocabularies;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class TenantVocabularyDto
{
    public string PresetId { get; set; } = VocabularyPresets.DefaultPresetId;
    public Dictionary<string, VocabularyTermDto> Terms { get; set; } = [];
}

public class VocabularyTermDto
{
    public string Singular           { get; set; } = string.Empty;
    public string Plural             { get; set; } = string.Empty;
    public string ArticleSingular    { get; set; } = "a";
    public string ArticlePlural      { get; set; } = "as";
    public string IndefiniteSingular { get; set; } = "uma";

    public static VocabularyTermDto FromTerm(VocabularyTerm t) => new()
    {
        Singular           = t.Singular,
        Plural             = t.Plural,
        ArticleSingular    = t.ArticleSingular,
        ArticlePlural      = t.ArticlePlural,
        IndefiniteSingular = t.IndefiniteSingular,
    };

    public static VocabularyTermDto FromDoc(VocabularyTermDoc d) => new()
    {
        Singular           = d.Singular,
        Plural             = d.Plural,
        ArticleSingular    = d.ArticleSingular,
        ArticlePlural      = d.ArticlePlural,
        IndefiniteSingular = d.IndefiniteSingular,
    };
}

public static class VocabularyResolver
{
    /// Merges the tenant's preset with any per-key overrides and returns the effective vocabulary.
    public static TenantVocabularyDto Resolve(TenantVocabulary? vocabulary)
    {
        var presetId = string.IsNullOrWhiteSpace(vocabulary?.PresetId)
            ? VocabularyPresets.DefaultPresetId
            : vocabulary!.PresetId;

        var preset = VocabularyPresets.GetPresetOrDefault(presetId);

        var terms = new Dictionary<string, VocabularyTermDto>();
        foreach (var key in LabelKeys.All)
        {
            if (vocabulary?.Overrides.TryGetValue(key, out var overrideDoc) == true)
            {
                terms[key] = VocabularyTermDto.FromDoc(overrideDoc);
            }
            else if (preset.TryGetValue(key, out var term))
            {
                terms[key] = VocabularyTermDto.FromTerm(term);
            }
            else
            {
                terms[key] = VocabularyTermDto.FromTerm(VocabularyPresets.GetDefaultTerm(key));
            }
        }

        return new TenantVocabularyDto
        {
            PresetId = presetId,
            Terms = terms,
        };
    }
}

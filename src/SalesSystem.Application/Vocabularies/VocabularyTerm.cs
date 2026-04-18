namespace SalesSystem.Application.Vocabularies;

public class VocabularyTerm
{
    public string Singular { get; set; } = string.Empty;
    public string Plural { get; set; } = string.Empty;
    public string ArticleSingular { get; set; } = "a";
    public string ArticlePlural { get; set; } = "as";
    public string IndefiniteSingular { get; set; } = "uma";

    public VocabularyTerm() { }

    public VocabularyTerm(string singular, string plural, string articleSingular = "a", string articlePlural = "as", string indefiniteSingular = "uma")
    {
        Singular = singular;
        Plural = plural;
        ArticleSingular = articleSingular;
        ArticlePlural = articlePlural;
        IndefiniteSingular = indefiniteSingular;
    }
}

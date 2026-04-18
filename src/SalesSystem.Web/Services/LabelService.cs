namespace SalesSystem.Web.Services;

public interface ILabelService
{
    string Singular(string key);
    string Plural(string key);
    string ArticleSingular(string key);
    string ArticlePlural(string key);
    string IndefiniteSingular(string key);

    /// "nova {key.singular}" with correct gender article. Returns e.g. "Nova Receita" / "Novo Pedido".
    string NewLabel(string key);

    /// "Editar {key.singular}" — same gender-agnostic prefix for both.
    string EditLabel(string key);

    /// "Nenhuma receita cadastrada." / "Nenhum pedido cadastrado." — gender-aware empty state.
    string EmptyStateMessage(string key);

    /// Full term, used rarely. Prefer Singular/Plural helpers.
    VocabularyTermDto Term(string key);
}

public class LabelService : ILabelService
{
    private readonly ApiService _api;

    public LabelService(ApiService api)
    {
        _api = api;
    }

    public VocabularyTermDto Term(string key)
    {
        if (_api.CurrentVocabulary?.Terms.TryGetValue(key, out var term) == true)
            return term;

        return _defaults.TryGetValue(key, out var fallback) ? fallback : FallbackTerm(key);
    }

    public string Singular(string key)           => Term(key).Singular;
    public string Plural(string key)             => Term(key).Plural;
    public string ArticleSingular(string key)    => Term(key).ArticleSingular;
    public string ArticlePlural(string key)      => Term(key).ArticlePlural;
    public string IndefiniteSingular(string key) => Term(key).IndefiniteSingular;

    public string NewLabel(string key)
    {
        var term = Term(key);
        var prefix = term.ArticleSingular == "o" ? "Novo" : "Nova";
        return $"{prefix} {term.Singular}";
    }

    public string EditLabel(string key) => $"Editar {Term(key).Singular}";

    public string EmptyStateMessage(string key)
    {
        var term = Term(key);
        var none = term.ArticleSingular == "o" ? "Nenhum" : "Nenhuma";
        var suffix = term.ArticleSingular == "o" ? "cadastrado" : "cadastrada";
        return $"{none} {term.Singular.ToLower()} {suffix}.";
    }

    private static VocabularyTermDto FallbackTerm(string key) => new()
    {
        Singular = key,
        Plural   = key,
    };

    /// Fallback vocabulary for prerender/pre-init phase (mirrors "confeitaria" preset on the server).
    private static readonly Dictionary<string, VocabularyTermDto> _defaults = new()
    {
        [LabelKeys.Recipe]     = new() { Singular = "Receita",        Plural = "Receitas",        ArticleSingular = "a", ArticlePlural = "as", IndefiniteSingular = "uma" },
        [LabelKeys.Ingredient] = new() { Singular = "Ingrediente",    Plural = "Ingredientes",    ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Yield]      = new() { Singular = "Rendimento",     Plural = "Rendimentos",     ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Process]    = new() { Singular = "Modo de Preparo",Plural = "Modos de Preparo",ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Insumo]     = new() { Singular = "Insumo",         Plural = "Insumos",         ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Product]    = new() { Singular = "Produto",        Plural = "Produtos",        ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Order]      = new() { Singular = "Pedido",         Plural = "Pedidos",         ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Customer]   = new() { Singular = "Cliente",        Plural = "Clientes",        ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Purchase]   = new() { Singular = "Compra",         Plural = "Compras",         ArticleSingular = "a", ArticlePlural = "as", IndefiniteSingular = "uma" },
        [LabelKeys.Production] = new() { Singular = "Produção",       Plural = "Produções",       ArticleSingular = "a", ArticlePlural = "as", IndefiniteSingular = "uma" },
        [LabelKeys.Stock]      = new() { Singular = "Estoque",        Plural = "Estoques",        ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
        [LabelKeys.Financial]  = new() { Singular = "Financeiro",     Plural = "Financeiros",     ArticleSingular = "o", ArticlePlural = "os", IndefiniteSingular = "um"  },
    };
}

public static class LabelKeys
{
    public const string Recipe     = "recipe";
    public const string Ingredient = "ingredient";
    public const string Yield      = "yield";
    public const string Process    = "process";
    public const string Insumo     = "insumo";
    public const string Product    = "product";
    public const string Order      = "order";
    public const string Customer   = "customer";
    public const string Purchase   = "purchase";
    public const string Production = "production";
    public const string Stock      = "stock";
    public const string Financial  = "financial";
}

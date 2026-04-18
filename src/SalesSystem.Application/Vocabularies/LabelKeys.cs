namespace SalesSystem.Application.Vocabularies;

public static class LabelKeys
{
    public const string Recipe      = "recipe";
    public const string Ingredient  = "ingredient";
    public const string Yield       = "yield";
    public const string Process     = "process";
    public const string Insumo      = "insumo";
    public const string Product     = "product";
    public const string Order       = "order";
    public const string Customer    = "customer";
    public const string Purchase    = "purchase";
    public const string Production  = "production";
    public const string Stock       = "stock";
    public const string Financial   = "financial";

    public static readonly IReadOnlyList<string> All =
    [
        Recipe, Ingredient, Yield, Process, Insumo, Product,
        Order, Customer, Purchase, Production, Stock, Financial
    ];
}

public static class VocabularyPresetIds
{
    public const string Confeitaria = "confeitaria";
    public const string Restaurante = "restaurante";
    public const string Grafica     = "grafica";
    public const string Brindes     = "brindes";
    public const string Generico    = "generico";

    public static readonly IReadOnlyList<string> All =
    [
        Confeitaria, Restaurante, Grafica, Brindes, Generico
    ];
}

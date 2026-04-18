namespace SalesSystem.Application.Vocabularies;

public static class VocabularyPresets
{
    public const string DefaultPresetId = VocabularyPresetIds.Confeitaria;

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, VocabularyTerm>> Presets =
        new Dictionary<string, IReadOnlyDictionary<string, VocabularyTerm>>
        {
            [VocabularyPresetIds.Confeitaria] = new Dictionary<string, VocabularyTerm>
            {
                [LabelKeys.Recipe]     = new("Receita", "Receitas", "a", "as", "uma"),
                [LabelKeys.Ingredient] = new("Ingrediente", "Ingredientes", "o", "os", "um"),
                [LabelKeys.Yield]      = new("Rendimento", "Rendimentos", "o", "os", "um"),
                [LabelKeys.Process]    = new("Modo de Preparo", "Modos de Preparo", "o", "os", "um"),
                [LabelKeys.Insumo]     = new("Insumo", "Insumos", "o", "os", "um"),
                [LabelKeys.Product]    = new("Produto", "Produtos", "o", "os", "um"),
                [LabelKeys.Order]      = new("Pedido", "Pedidos", "o", "os", "um"),
                [LabelKeys.Customer]   = new("Cliente", "Clientes", "o", "os", "um"),
                [LabelKeys.Purchase]   = new("Compra", "Compras", "a", "as", "uma"),
                [LabelKeys.Production] = new("Produção", "Produções", "a", "as", "uma"),
                [LabelKeys.Stock]      = new("Estoque", "Estoques", "o", "os", "um"),
                [LabelKeys.Financial]  = new("Financeiro", "Financeiros", "o", "os", "um"),
            },

            [VocabularyPresetIds.Restaurante] = new Dictionary<string, VocabularyTerm>
            {
                [LabelKeys.Recipe]     = new("Ficha Técnica", "Fichas Técnicas", "a", "as", "uma"),
                [LabelKeys.Ingredient] = new("Ingrediente", "Ingredientes", "o", "os", "um"),
                [LabelKeys.Yield]      = new("Rendimento", "Rendimentos", "o", "os", "um"),
                [LabelKeys.Process]    = new("Modo de Preparo", "Modos de Preparo", "o", "os", "um"),
                [LabelKeys.Insumo]     = new("Insumo", "Insumos", "o", "os", "um"),
                [LabelKeys.Product]    = new("Prato", "Pratos", "o", "os", "um"),
                [LabelKeys.Order]      = new("Comanda", "Comandas", "a", "as", "uma"),
                [LabelKeys.Customer]   = new("Cliente", "Clientes", "o", "os", "um"),
                [LabelKeys.Purchase]   = new("Compra", "Compras", "a", "as", "uma"),
                [LabelKeys.Production] = new("Preparo", "Preparos", "o", "os", "um"),
                [LabelKeys.Stock]      = new("Estoque", "Estoques", "o", "os", "um"),
                [LabelKeys.Financial]  = new("Financeiro", "Financeiros", "o", "os", "um"),
            },

            [VocabularyPresetIds.Grafica] = new Dictionary<string, VocabularyTerm>
            {
                [LabelKeys.Recipe]     = new("Ficha Técnica", "Fichas Técnicas", "a", "as", "uma"),
                [LabelKeys.Ingredient] = new("Componente", "Componentes", "o", "os", "um"),
                [LabelKeys.Yield]      = new("Tiragem", "Tiragens", "a", "as", "uma"),
                [LabelKeys.Process]    = new("Processo", "Processos", "o", "os", "um"),
                [LabelKeys.Insumo]     = new("Matéria-Prima", "Matérias-Primas", "a", "as", "uma"),
                [LabelKeys.Product]    = new("Produto", "Produtos", "o", "os", "um"),
                [LabelKeys.Order]      = new("Orçamento", "Orçamentos", "o", "os", "um"),
                [LabelKeys.Customer]   = new("Cliente", "Clientes", "o", "os", "um"),
                [LabelKeys.Purchase]   = new("Compra", "Compras", "a", "as", "uma"),
                [LabelKeys.Production] = new("Produção", "Produções", "a", "as", "uma"),
                [LabelKeys.Stock]      = new("Estoque", "Estoques", "o", "os", "um"),
                [LabelKeys.Financial]  = new("Financeiro", "Financeiros", "o", "os", "um"),
            },

            [VocabularyPresetIds.Brindes] = new Dictionary<string, VocabularyTerm>
            {
                [LabelKeys.Recipe]     = new("Composição", "Composições", "a", "as", "uma"),
                [LabelKeys.Ingredient] = new("Item", "Itens", "o", "os", "um"),
                [LabelKeys.Yield]      = new("Quantidade", "Quantidades", "a", "as", "uma"),
                [LabelKeys.Process]    = new("Montagem", "Montagens", "a", "as", "uma"),
                [LabelKeys.Insumo]     = new("Material", "Materiais", "o", "os", "um"),
                [LabelKeys.Product]    = new("Kit", "Kits", "o", "os", "um"),
                [LabelKeys.Order]      = new("Pedido", "Pedidos", "o", "os", "um"),
                [LabelKeys.Customer]   = new("Cliente", "Clientes", "o", "os", "um"),
                [LabelKeys.Purchase]   = new("Compra", "Compras", "a", "as", "uma"),
                [LabelKeys.Production] = new("Montagem", "Montagens", "a", "as", "uma"),
                [LabelKeys.Stock]      = new("Estoque", "Estoques", "o", "os", "um"),
                [LabelKeys.Financial]  = new("Financeiro", "Financeiros", "o", "os", "um"),
            },

            [VocabularyPresetIds.Generico] = new Dictionary<string, VocabularyTerm>
            {
                [LabelKeys.Recipe]     = new("Ficha Técnica", "Fichas Técnicas", "a", "as", "uma"),
                [LabelKeys.Ingredient] = new("Componente", "Componentes", "o", "os", "um"),
                [LabelKeys.Yield]      = new("Quantidade Produzida", "Quantidades Produzidas", "a", "as", "uma"),
                [LabelKeys.Process]    = new("Processo", "Processos", "o", "os", "um"),
                [LabelKeys.Insumo]     = new("Matéria-Prima", "Matérias-Primas", "a", "as", "uma"),
                [LabelKeys.Product]    = new("Produto", "Produtos", "o", "os", "um"),
                [LabelKeys.Order]      = new("Pedido", "Pedidos", "o", "os", "um"),
                [LabelKeys.Customer]   = new("Cliente", "Clientes", "o", "os", "um"),
                [LabelKeys.Purchase]   = new("Compra", "Compras", "a", "as", "uma"),
                [LabelKeys.Production] = new("Produção", "Produções", "a", "as", "uma"),
                [LabelKeys.Stock]      = new("Estoque", "Estoques", "o", "os", "um"),
                [LabelKeys.Financial]  = new("Financeiro", "Financeiros", "o", "os", "um"),
            },
        };

    public static IReadOnlyDictionary<string, VocabularyTerm> GetPresetOrDefault(string? presetId)
    {
        if (!string.IsNullOrWhiteSpace(presetId) && Presets.TryGetValue(presetId, out var preset))
            return preset;
        return Presets[DefaultPresetId];
    }

    public static VocabularyTerm GetDefaultTerm(string labelKey)
    {
        var preset = Presets[DefaultPresetId];
        return preset.TryGetValue(labelKey, out var term)
            ? term
            : new VocabularyTerm(labelKey, labelKey);
    }
}

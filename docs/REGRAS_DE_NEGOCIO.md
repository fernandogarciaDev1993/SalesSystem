# MR Delicias - Sistema de Vendas
## Documentacao de Regras de Negocio e Fluxos

---

## 1. Visao Geral

Sistema de gestao para confeitaria que controla todo o ciclo: **compra de insumos** -> **producao de receitas** -> **venda de produtos** -> **controle financeiro**.

### Arquitetura
- **Backend**: .NET 9 + MongoDB Atlas
- **Frontend**: Blazor Server
- **Multi-tenant**: Cada empresa opera isolada

---

## 2. Entidades do Sistema

### 2.1 Insumo (Materia-Prima)
O que voce **compra** no mercado.

| Campo | Descricao |
|-------|-----------|
| Codigo | Auto-gerado (00001, 00002...) |
| Nome | Ex: "Farinha de Trigo" |
| Unidade | KG, LT, UN, GR, ML, MT |
| Estoque | Armazenado na **menor unidade** (g, ml, un) |
| Custo Medio | Custo ponderado por unidade-base |
| Vendavel | Se marcado, cria produto automaticamente |

**Regra de conversao**:
```
1 KG = 1000 gramas (armazenado)
1 LT = 1000 mililitros (armazenado)
1 UN = 1 unidade (sem conversao)
```

### 2.2 Compra de Insumo
Registro de cada compra realizada.

**Campos de entrada**:
- Quantidade de embalagens
- Conteudo por embalagem (g, ml, un)
- Preco por embalagem

**Calculo automatico**:
```
Total unidades-base = embalagens x conteudo_por_embalagem
Custo por unidade-base = preco_embalagem / conteudo_por_embalagem
```

**Exemplo**: 2 pacotes de farinha (1000g cada) por R$5/pacote
```
Total = 2 x 1000 = 2000g
Custo/g = R$5 / 1000 = R$0,005/g
```

**Custo Medio Ponderado** (atualizado a cada compra):
```
custo_total_anterior = estoque_atual x custo_medio_atual
custo_total_novo = quantidade_comprada x custo_unitario
novo_estoque = estoque_atual + quantidade_comprada
novo_custo_medio = (custo_total_anterior + custo_total_novo) / novo_estoque
```

**Controle FIFO**: Cada compra tem `estoque_restante`. Ao consumir, desconta do lote mais antigo primeiro.

### 2.3 Receita
Formula de producao que transforma insumos em produto.

| Campo | Descricao |
|-------|-----------|
| Codigo | Auto-gerado (REC-00001) |
| Nome | Ex: "Bolo de Baunilha" |
| Ingredientes | Lista de insumos + quantidades (em g, ml, un) |
| Modo de Preparo | Passos numerados |
| Rendimento | Quantas unidades produz por lote |
| Custo Calculado | Soma dos custos dos ingredientes |
| Custo/Unidade | Custo calculado / rendimento |
| E Insumo? | Se sim, o resultado vira insumo para outras receitas |

**Custo da receita** (recalcula automaticamente quando insumos mudam):
```
custo_total = soma(quantidade_ingrediente x custo_por_unidade_base)
custo_por_unidade = custo_total / rendimento
```

**Sub-receitas**: Uma receita pode usar outra receita como ingrediente.
```
Exemplo:
  Receita "Massa Base" (gera insumo) -> usa farinha + ovos + leite
  Receita "Bolo de Pote" (gera produto) -> usa "Massa Base" + chocolate
```

### 2.4 Produto
O que voce **vende** ao cliente.

| Campo | Descricao |
|-------|-----------|
| SKU | Auto-gerado (000001) |
| Nome | Ex: "Bolo de Pote de Chocolate" |
| Tipo | Produzido (receita) / Revenda (insumo direto) |
| Custo | Preenchido pela receita ou manual |
| Custos Operacionais % | Aluguel, agua, luz, mao-de-obra |
| Margem de Lucro % | Lucro desejado |
| Impostos % | Taxa de impostos |
| Preco de Venda | Calculado automaticamente |

**Formula de precificacao (Markup Divisor)**:
```
Preco = Custo / (1 - Operacional% - Lucro% - Impostos%)

Exemplo:
  Custo = R$1,40
  Operacional = 25%
  Lucro = 30%
  Impostos = 10%
  Preco = 1,40 / (1 - 0,25 - 0,30 - 0,10) = 1,40 / 0,35 = R$4,00
```

**Composicao do preco**:
```
  Custo:        R$1,40 (35%)
  Operacional:  R$1,00 (25%)
  Lucro:        R$1,20 (30%)
  Impostos:     R$0,40 (10%)
  Total:        R$4,00 (100%)
```

### 2.5 Pedido
Registro de venda ao cliente.

| Status | Descricao |
|--------|-----------|
| Rascunho | Pedido criado, ainda nao confirmado |
| Confirmado | Estoque descontado, financeiro registrado |
| Cancelado | Estoque devolvido |

**Calculos do pedido**:
```
subtotal = soma(preco_unitario x quantidade)
desconto_total = soma(desconto_por_item x quantidade)
total = subtotal - desconto_total + impostos + frete
margem_bruta = total - custo_total
margem_% = (margem_bruta / total) x 100
```

### 2.6 Financeiro
Controle de receitas e despesas.

| Campo | Descricao |
|-------|-----------|
| Codigo | Auto-gerado (FIN-00001) |
| Tipo | Receita ou Despesa |
| Categoria | Venda, Compra, Aluguel, Gasolina, etc. |
| Status | Pendente, Pago, Vencido, Cancelado |

**Entradas automaticas**:
- Compra de insumo -> Despesa (categoria Compra)
- Confirmacao de pedido -> Receita (categoria Venda)

### 2.7 Cliente
| Campo | Descricao |
|-------|-----------|
| Codigo | Auto-gerado (CLI-00001) |
| Tipo | PF (Pessoa Fisica) ou PJ (Pessoa Juridica) |
| Aniversario | Alerta ao criar pedido no dia |

---

## 3. Fluxos de Negocio

### 3.1 Fluxo de Compra
```
+------------------+     +-------------------+     +------------------+
|  Compra no       | --> |  Registra Insumo  | --> |  Estoque do      |
|  Mercado         |     |  + Compra Inicial |     |  Insumo Atualiza |
+------------------+     +-------------------+     +------------------+
                                  |
                                  v
                          +-------------------+
                          |  Lancamento       |
                          |  Financeiro       |
                          |  (Despesa/Compra) |
                          +-------------------+
```

**Detalhamento**:
1. Cadastra insumo com nome, unidade, quantidade e preco
2. Sistema converte para unidade-base (g, ml, un)
3. Calcula custo medio ponderado
4. Registra compra com rastreamento FIFO
5. Cria lancamento financeiro automatico (Despesa)
6. Se marcado como vendavel: cria Produto + Estoque do Produto

### 3.2 Fluxo de Receita
```
+------------------+     +-------------------+     +------------------+
|  Seleciona       | --> |  Define           | --> |  Custo Calculado |
|  Ingredientes    |     |  Quantidades (g)  |     |  Automaticamente |
+------------------+     +-------------------+     +------------------+
                                  |
                                  v
                          +-------------------+
                          |  Modo de Preparo  |
                          |  (Passo a Passo)  |
                          +-------------------+
```

**Tipos de receita**:
```
Receita INSUMO:
  Entrada: insumos do estoque
  Saida: novo insumo (para usar em outras receitas)
  Exemplo: "Massa Base" -> vira insumo

Receita PRODUTO:
  Entrada: insumos do estoque
  Saida: produto para venda
  Exemplo: "Bolo de Pote" -> vira produto vendavel
```

### 3.3 Fluxo de Producao
```
+------------------+     +-------------------+     +------------------+
|  Seleciona       | --> |  Valida Estoque   | --> |  Consome         |
|  Receita         |     |  de TODOS Insumos |     |  Insumos (FIFO)  |
+------------------+     +-------------------+     +------------------+
                                                           |
                              +----------------------------+
                              |
                              v
                    +--------------------+
                    |  Receita IsInsumo? |
                    +--------------------+
                       /            \
                     SIM            NAO
                      |              |
                      v              v
              +-----------+   +-----------+
              | Adiciona  |   | Adiciona  |
              | Estoque   |   | Estoque   |
              | INSUMO    |   | PRODUTO   |
              +-----------+   +-----------+
```

**Validacao pre-producao** (2 passagens):
```
PASSAGEM 1 - VALIDACAO:
  Para cada ingrediente:
    - Se insumo: verificar estoque >= quantidade necessaria
    - Se sub-receita: verificar estoque do insumo de saida >= quantidade
    - Se qualquer falhar: BLOQUEIA TUDO (nada e consumido)

PASSAGEM 2 - CONSUMO (so executa se validacao passou):
  Para cada ingrediente:
    - Desconta do estoque (FIFO - lote mais antigo primeiro)
```

### 3.4 Fluxo de Venda
```
+------------------+     +-------------------+     +------------------+
|  Cria Pedido     | --> |  Adiciona Itens   | --> |  Confirma        |
|  (Rascunho)      |     |  + Descontos      |     |  Pedido          |
+------------------+     +-------------------+     +------------------+
                                                           |
              +--------------------------------------------+
              |
              v
    +--------------------+
    |  Valida Estoque    |
    |  de TODOS Produtos |
    +--------------------+
              |
              v
    +--------------------+     +-------------------+
    |  Desconta Estoque  | --> |  Registra Receita |
    |  dos Produtos      |     |  no Financeiro    |
    +--------------------+     +-------------------+
```

**Alertas na criacao do pedido**:
- Aniversario do cliente: mostra aviso especial
- Estoque insuficiente: aviso ao adicionar item
- Desconto acima do lucro: aviso quando desconto > margem

**Descontos**:
```
Desconto por item: R$ por unidade
  Preco efetivo = preco_unitario - desconto_por_unidade
  Total desconto item = desconto_por_unidade x quantidade

Desconto geral: % sobre subtotal
  Valor desconto = subtotal x (percentual / 100)
```

### 3.5 Fluxo Financeiro
```
+------------------+     +-------------------+
|  ENTRADAS        |     |  SAIDAS           |
|  AUTOMATICAS     |     |  AUTOMATICAS      |
+------------------+     +-------------------+
|                  |     |                   |
|  Venda Confirmada| --> |  Compra Insumo    |
|  (Receita/Venda) |     |  (Despesa/Compra) |
+------------------+     +-------------------+

+------------------+
|  ENTRADAS        |
|  MANUAIS         |
+------------------+
|  Aporte          |
|  Aluguel         |
|  Gasolina        |
|  Salarios        |
|  Caixa Inicial   |
|  Outros          |
+------------------+
```

**Relatorio Mensal**:
```
Receitas = soma(tipo=Receita no periodo)
Despesas = soma(tipo=Despesa no periodo)
Saldo = Receitas - Despesas
Pendentes = count(status=Pendente)
Vencidos = count(status=Vencido)
```

---

## 4. Controle de Estoque

### 4.1 Estoque de Insumos
- Armazenado em unidade-base (g, ml, un)
- Consumo FIFO (First In, First Out)
- Alerta quando abaixo do minimo

### 4.2 Estoque de Produtos
- Controlado por StockBalance/StockMove
- Entrada via producao ou compra de revenda
- Saida via confirmacao de pedido

### 4.3 FIFO (Primeiro a Entrar, Primeiro a Sair)
```
Compra 1: 1000g farinha a R$0,005/g (RemainingStock: 1000)
Compra 2: 500g farinha a R$0,006/g  (RemainingStock: 500)

Consumo de 800g:
  -> Compra 1: consome 800g (RemainingStock: 200)
  -> Compra 2: nao tocada (RemainingStock: 500)

Consumo de 400g:
  -> Compra 1: consome 200g (RemainingStock: 0) [ESGOTADA]
  -> Compra 2: consome 200g (RemainingStock: 300)
```

### 4.4 Deletar Compra
- So permite se **nada foi consumido** (RemainingStock == Quantity)
- Reverte estoque do insumo
- Deleta lancamento financeiro vinculado
- Recalcula custo medio dos lotes restantes

---

## 5. Codigos Auto-Gerados

| Entidade | Formato | Exemplo |
|----------|---------|---------|
| Insumo | 5 digitos | 00001 |
| Produto | 6 digitos | 000001 |
| Cliente | CLI-XXXXX | CLI-00001 |
| Receita | REC-XXXXX | REC-00001 |
| Pedido | PED-XXXXXX | PED-000001 |
| Financeiro | FIN-XXXXX | FIN-00001 |

**Regra**: Busca o maior codigo numerico existente no banco e incrementa +1.

---

## 6. Autenticacao e Permissoes

### Papeis
| Papel | Acesso |
|-------|--------|
| Admin | Tudo |
| Gerente | Tudo exceto config e usuarios |
| Vendedor | Produtos (leitura), Clientes, Pedidos |
| Estoquista | Produtos, Estoque |
| Financeiro | Financeiro, Pedidos (leitura) |
| Viewer | Somente leitura |

### Fluxo de Login
```
1. Usuario envia email + senha + tenant (subdomain)
2. Sistema resolve subdomain -> tenant ID
3. Valida credenciais (BCrypt)
4. Gera token JWT (expira em 60 min)
5. Gera refresh token (expira em 7 dias)
6. Token armazenado no sessionStorage do browser
```

---

## 7. Cenarios de Uso

### Cenario 1: Compra e Revenda Direta
```
Coca-Cola 200ml:
1. Cadastra insumo "Coca-Cola 200ml" (UN)
2. Marca "disponivel para venda"
3. Informa: 1 fardo, 12 unidades, R$20/fardo
4. Sistema cria:
   - Insumo com 12 un, custo R$1,67/un
   - Produto "Coca-Cola 200ml" com estoque 12
   - Financeiro: Despesa R$20
5. Vende via pedido -> desconta estoque
```

### Cenario 2: Producao em Cadeia
```
1. Compra insumos: farinha, ovos, leite, baunilha, chocolate
2. Cria receita "Massa Base" (marcada como insumo)
   - 300g farinha + 4 ovos + 200ml leite + 1ml baunilha
   - Rendimento: 10 unidades
3. Produz "Massa Base" -> gera 10 un no estoque de insumos
4. Cria receita "Bolo de Pote" (produto para venda)
   - 1 un Massa Base + 100g chocolate
   - Rendimento: 1 unidade
5. Cria produto "Bolo de Pote" vinculado a receita
6. Produz "Bolo de Pote" -> consome massa base + chocolate -> gera estoque produto
7. Vende via pedido
```

### Cenario 3: Desconto no Aniversario
```
1. Cliente cadastrado com data de nascimento
2. No dia do aniversario, ao criar pedido:
   - Sistema mostra alerta de aniversario
   - Vendedor aplica desconto especial
   - Se desconto > margem de lucro: aviso
```

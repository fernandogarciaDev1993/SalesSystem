# SalesSystem

Sistema de controle de vendas multi-tenant â€” .NET 8 + Blazor Server + MongoDB.

## Inicio rapido

```powershell
docker-compose up -d
dotnet restore
dotnet run --project src/SalesSystem.Api
dotnet run --project src/SalesSystem.Web
```

## Modulos
- Produtos | Clientes | Estoque | Pedidos | Financeiro
- Multi-tenant UI configuravel por banco
- Autenticacao JWT com permissoes granulares
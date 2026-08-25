# Bebidas API

API REST para gerenciamento de bebidas e categorias, desenvolvida com ASP.NET Core, Entity Framework Core e SQL Server.

## Tecnologias

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- AutoMapper
- Swagger/OpenAPI

## Funcionalidades

- CRUD de bebidas
- CRUD de categorias
- Criação de bebidas em lote
- DTOs e validações
- Filtros por nome, categoria e preço
- Ordenação dinâmica
- Paginação
- Services e interfaces
- Dependency Injection
- AutoMapper
- Tratamento global de exceções

## Como executar

1. Clone o repositório.
2. Configure a connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "SUA_CONNECTION_STRING"
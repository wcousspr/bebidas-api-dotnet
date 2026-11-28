# Bebidas API

API REST para gerenciamento de bebidas e categorias, desenvolvida com **ASP.NET Core 10 LTS**, **Entity Framework Core** e **SQL Server**. O projeto inclui autenticação JWT, autorização por roles, testes automatizados e execução com Docker Compose.

## Tecnologias

- C# e .NET 10 LTS / ASP.NET Core Web API
- Entity Framework Core e SQL Server 2022
- ASP.NET Core Identity e JWT Bearer
- AutoMapper
- Swagger / OpenAPI
- Docker e Docker Compose
- xUnit, Moq e testes de integração
- GitHub Actions (CI)

## Funcionalidades

- CRUD de bebidas e categorias
- Cadastro de bebidas em lote
- DTOs e validações
- Filtros por nome, categoria, preço e quantidade
- Ordenação e paginação
- Camada de services, interfaces e injeção de dependência
- Mapeamento com AutoMapper
- Tratamento global de exceções e logging
- Cadastro e login de usuários com JWT
- Autorização por roles **Admin** e **User**
- Gerenciamento de usuários e permissões administrativas

## Estrutura do projeto

- `crudEbancoDist8/`: aplicação ASP.NET Core Web API
- `crudEbancoDist8.Tests/`: testes unitários
- `crudEbancoDist8.IntegrationTests/`: testes de integração
- `Dockerfile`: imagem da API
- `compose.yaml`: serviços da API e do SQL Server

## Pré-requisitos

- .NET SDK 10.0.404, conforme `global.json`, para build, testes e uso do EF fora do Docker
- Docker Desktop com Docker Compose

Na raiz, confirme o SDK selecionado e compile a solução. O `global.json` aceita patches estáveis na faixa 10.0.4xx.

```powershell
dotnet --version
dotnet restore crudEbancoDist8.slnx
dotnet build crudEbancoDist8.slnx --configuration Release --no-restore
```

O manifesto local fixa a ferramenta EF Core. Restaure-a dentro do projeto da API para usar a versão do repositório:

```powershell
Push-Location .\crudEbancoDist8
dotnet tool restore
dotnet tool run dotnet-ef --version
Pop-Location
```

## Executando com Docker Compose (desenvolvimento local)

### 1. Configure as variáveis

Na raiz do repositório, crie um arquivo `.env` (não o envie ao Git):

```dotenv
SQL_SA_PASSWORD=SUBSTITUA_POR_SENHA_FORTE
JWT_KEY=SUBSTITUA_POR_CHAVE_ALEATORIA_LONGA
JWT_ISSUER=BebidasApi
JWT_AUDIENCE=BebidasApiUsers
```

Use valores reais e seguros. `JWT_ISSUER` e `JWT_AUDIENCE` devem corresponder às configurações usadas para emitir e validar tokens. O `.env` é destinado apenas ao desenvolvimento local; não é um mecanismo de gerenciamento de segredos para produção.

### 2. Inicie somente o SQL Server

```powershell
docker compose up -d sqlserver
docker compose ps
```

Aguarde o SQL Server estar saudável antes de continuar.

### 3. Aplique as migrations no banco Docker

No **PowerShell do Windows**, na raiz do repositório, carregue a senha do `.env` e defina a connection string **apenas para esta sessão**:

```powershell
$env:SQL_SA_PASSWORD = (Get-Content .env | Where-Object { $_ -match '^SQL_SA_PASSWORD=' } | Select-Object -First 1) -replace '^SQL_SA_PASSWORD=', ''
$env:ConnectionStrings__DefaultConnection = "Server=tcp:127.0.0.1,1433;Database=DistribuidoraDb;User Id=sa;Password=$env:SQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"

Push-Location .\crudEbancoDist8
dotnet tool run dotnet-ef database update
Pop-Location
```

> Esse comando é voltado ao ambiente local Windows, com a porta 1433 do banco Docker publicada em `127.0.0.1`. Não use a connection string do LocalDB ao aplicar migrations no Docker.

### 4. Inicie a API

```powershell
docker compose up -d --build
docker compose ps
```

Com os serviços em execução, abra o Swagger em **http://localhost:8080/swagger**.

Para verificar logs:

```powershell
docker compose logs --tail=50 api
```

> O banco Docker é independente do LocalDB. O seed inicializa as roles `Admin` e `User`. Para promover um usuário em um banco local que já possui um administrador, autentique-se com essa conta e use `POST /api/account/users/{userId}/roles/admin`. Um banco local novo não tem bootstrap automático de administrador. O provisionamento da primeira conta precisa de um procedimento explícito e aprovado para esse banco isolado.

## Executando sem Docker

Configure a connection string e as opções JWT por User Secrets (ou outro provedor seguro de configuração):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "SUA_CONNECTION_STRING" --project .\crudEbancoDist8
dotnet user-secrets set "Jwt:Key" "SUA_CHAVE_JWT" --project .\crudEbancoDist8
dotnet user-secrets set "Jwt:Issuer" "SEU_ISSUER" --project .\crudEbancoDist8
dotnet user-secrets set "Jwt:Audience" "SUA_AUDIENCE" --project .\crudEbancoDist8
```

Para inicializar um banco de desenvolvimento vazio, aplique as migrations existentes e execute a API:

```powershell
Push-Location .\crudEbancoDist8
dotnet tool run dotnet-ef database update
Pop-Location
dotnet run --project .\crudEbancoDist8
```

## Autenticação e autorização

A API usa **ASP.NET Core Identity** para usuários e roles, e **JWT Bearer** para autenticação das requisições. Após fazer login, envie o token no cabeçalho `Authorization: Bearer <token>` ou utilize o botão **Authorize** do Swagger.

Endpoints protegidos podem exigir a role **Admin**. Uma conta recém-cadastrada não deve receber privilégios administrativos automaticamente.

## Testes e integração contínua

Após restore e build Release, execute os testes unitários e de integração a partir da raiz:

```powershell
dotnet test crudEbancoDist8.slnx --configuration Release --no-build --no-restore
dotnet package list --project crudEbancoDist8.slnx --include-transitive --no-restore
dotnet package list --project crudEbancoDist8.slnx --include-transitive --vulnerable --no-restore
```

O GitHub Actions usa o SDK definido em `global.json`, audita dependências diretas e transitivas e executa o build e os testes. Falhas de acesso ao feed ou aos dados de advisories invalidam a verificação.

## Validação da migração para .NET 10

Use configuração de teste e um SQL Server isolado. Após o build Release, verifique o modelo contra o snapshot e gere o SQL para inspeção:

```powershell
Push-Location .\crudEbancoDist8
dotnet tool run dotnet-ef migrations has-pending-model-changes --context AppDbContext --configuration Release --no-build
dotnet tool run dotnet-ef migrations script --idempotent --context AppDbContext --configuration Release --no-build
Pop-Location
```

A geração do script serve para revisão. Se houver divergência no modelo, identifique a alteração antes de criar uma migration. Em banco isolado, valide a cadeia existente, a precisão do preço, as chaves estrangeiras, o schema do Identity, o cadastro em lote e os resultados de filtros e paginação. Exercite as versões antiga e nova sobre o mesmo schema para confirmar a reversibilidade.

Depois de iniciar a API com o Compose, confirme a configuração do container e a leitura pública:

```powershell
docker compose exec -T api dotnet --list-runtimes
docker compose exec -T api id
docker compose port api 8080
docker compose logs --tail=50 api
curl.exe --fail --silent --show-error http://localhost:8080/api/bebidas
```

Em staging, compare os contratos HTTP com a versão anterior, incluindo tokens emitidos nos dois runtimes sob a mesma configuração de teste, autenticação, CRUD, CORS, uploads e Open Graph. Registre o artefato da versão anterior e as configurações de publicação. Se a nova versão falhar e o schema estiver preservado, restaure a imagem anterior e repita as verificações de saúde e leitura. Uma mudança de schema exige um plano próprio de compatibilidade entre versões.

## Segurança e observações

- Nunca publique `.env`, senhas do SQL Server, chaves JWT ou arquivos de User Secrets.
- Não utilize credenciais de desenvolvimento em produção.
- O SQL Server é publicado apenas na interface local (`127.0.0.1:1433`) na configuração de desenvolvimento.
- O Swagger está habilitado no ambiente `Development`; revise a configuração antes de fazer deploy.
- O banco utiliza volume persistente do Docker. **`docker compose down -v` remove o volume e pode apagar os dados.**
- As migrations não são executadas automaticamente pelo Compose descrito aqui; aplique-as antes da primeira inicialização da API em um banco vazio.

## Status

Projeto backend em evolução, com ambiente Docker local, autenticação, autorização, testes e CI. Próximas etapas: revisão de segurança para produção, deploy e frontend integrado.

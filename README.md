# Bebidas API

[![Build e testes](https://github.com/wcousspr/bebidas-api-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/wcousspr/bebidas-api-dotnet/actions/workflows/ci.yml)

API REST de catálogo e estoque de bebidas. Um usuário autenticado pode consultar bebidas por nome, categoria, preço e quantidade. Administradores também podem cadastrar bebidas em lote, atualizar o estoque e gerenciar categorias. Uma categoria com bebidas vinculadas não pode ser excluída: a API retorna **409 Conflict**.

## Executar localmente no Windows

Use PowerShell, Git, SQL Server LocalDB (instância `MSSQLLocalDB`), SDK .NET 10 para a solução `.slnx` e .NET 8 para executar a API e os testes. Instalar os SDKs 8 e 10 reproduz a combinação usada no CI.

### 1. Baixe e restaure a solução

```powershell
git clone https://github.com/wcousspr/bebidas-api-dotnet.git
cd bebidas-api-dotnet
dotnet restore crudEbancoDist8.slnx
cd crudEbancoDist8
dotnet tool restore
```

Os comandos seguintes são executados na pasta do projeto da API, onde ficam o arquivo `.csproj` e o manifesto do EF Core.

### 2. Configure o banco e a assinatura dos tokens

Em um clone novo, configure os User Secrets no seu computador:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=BebidasDb;Trusted_Connection=True;TrustServerCertificate=True"

$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes))
```

Gere a chave apenas na configuração inicial: trocá-la invalida tokens já emitidos. Emissor, audiência e duração do token estão em `appsettings.json`. Mantenha chaves reais e credenciais fora do Git.

### 3. Aplique as migrations e abra o Swagger

```powershell
dotnet ef database update -- --environment Development
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

Com a API em execução, abra **https://localhost:7113/swagger**. Se a porta exibida em `Now listening on` for diferente, use a porta do terminal. O Swagger é habilitado em Development.

### 4. Faça sua primeira consulta

1. Use `POST /api/Auth/register` com `userName`, `email` e `password`. A senha deve ter pelo menos seis caracteres, incluindo maiúscula, minúscula e número.
2. Em `POST /api/Auth/login`, envie o e-mail e a senha. Copie o campo `token` da resposta.
3. No Swagger, clique em **Authorize** e cole apenas o token. Execute `GET /api/Auth/me` para conferir sua identidade e suas roles.
4. Experimente `GET /api/Bebidas/paginado?pageNumber=1&pageSize=10&orderBy=quantity&direction=desc&minQuantity=5`. Um banco novo pode retornar uma página vazia com sucesso.

## Permissões

| Operação | Acesso |
|---|---|
| Cadastro e login | Público |
| Perfil e consultas de bebidas, incluindo paginação | Usuário autenticado |
| Criação, atualização, exclusão e criação em lote de bebidas | Admin |
| Consultas e alterações de categorias | Admin |
| Listar usuários e promover uma conta a Admin | Admin |

**Primeiro Admin local:** cadastre sua própria conta e copie o ID retornado no cadastro. Pare a API, execute os comandos abaixo na pasta do projeto e aguarde a inicialização em Development:

```powershell
dotnet user-secrets set "Seed:AdminUserId" "ID_DA_SUA_CONTA"
dotnet run --launch-profile https
```

Após a inicialização, remova a configuração com `dotnet user-secrets remove "Seed:AdminUserId"` em outro terminal na mesma pasta. Faça login novamente para receber um token com a nova role. Como Admin, crie uma categoria e use o ID dela no campo `categoryId` ao cadastrar a primeira bebida.

## Testes e integração contínua

Na raiz, onde está `crudEbancoDist8.slnx`:

```powershell
dotnet test crudEbancoDist8.slnx --configuration Release
```

Os testes unitários usam xUnit e Moq para verificar regras dos serviços e respostas dos controllers. Os testes de integração usam requisições HTTP com autenticação simulada para verificar permissões e SQLite em memória para verificar consultas, exclusões e integridade dos dados. A suíte não depende do LocalDB da aplicação.

O workflow **Build and Tests** restaura, compila e testa a solução em Linux nos pushes e pull requests destinados à main.

## Organização do código

- `crudEbancoDist8/`: API ASP.NET Core 8. Controllers recebem as requisições; serviços implementam operações do catálogo e de usuários. EF Core persiste os dados no SQL Server; Identity gerencia usuários e roles; a API autentica requisições com JWT.
- `crudEbancoDist8.Tests/`: testes unitários de controllers e serviços.
- `crudEbancoDist8.IntegrationTests/`: testes de integração HTTP e com banco temporário.

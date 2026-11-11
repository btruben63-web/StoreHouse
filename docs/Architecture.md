# Architecture

Documento baseado exclusivamente no código atual. Para instalação e operação, ver [Runbook.md](Runbook.md); para histórico, ver [CHANGELOG.md](CHANGELOG.md).

## 1. Visão geral

StoreHouse é uma solução .NET 10 (C# 14) com dois serviços HTTP independentes e uma base de dados PostgreSQL por serviço:

- **App.IdentityServer** autentica utilizadores e emite tokens JWT.
- **Product.Api** expõe a gestão de produtos e stock (9 endpoints em `/api/products`) e valida os tokens emitidos pelo IdentityServer.

A descrição funcional dos endpoints e os utilizadores de demonstração estão no [README](../README.md).

## 2. Componentes

| Componente | Responsabilidade |
|---|---|
| `App.IdentityServer` | Duende IdentityServer + ASP.NET Identity (utilizadores, roles, lockout). Expõe `POST /api/auth/login` e os endpoints OIDC (`/connect/token`, `/.well-known/openid-configuration`). |
| `Product.Api` | Camada de apresentação: controllers, autenticação JWT Bearer, autorização por role, Swagger, tratamento global de erros (`ProblemDetails`). |
| `Product.Application` | Casos de uso (CQRS com MediatR), validação (FluentValidation), DTOs, mapeamento (Mapster), padrão `Result`, contratos de persistência. Não depende de HTTP nem de EF Core. |
| `Product.Domain` | Entidades (modelo anémico), enum `StockMovementType` e constantes de validação (`ProductConstraints`). |
| `Product.Infrastructure` | EF Core + Npgsql: `ProductDbContext`, configurações de tabelas, migrations e seed, repositório, queries de leitura, `UnitOfWork`. |
| `Product.Api.UnitTest` | Testes unitários (xUnit v3 + Moq) sobre `Product.Api` e `Product.Application`. |

## 3. Estrutura de pastas

```
IdentityServer/App.IdentityServer/
  Authorization/      roles (Admin, Manager)
  Configuration/      IdentityServerSettings, clients/scopes/resources (em memória)
  Controllers/        AuthController (login)
  Data/               AppIdentityDbContext, Migrations, Seed (roles, utilizadores, claims)
  Services/           DistinctClaimsProfileService
Product/
  Product.Domain/           Entities, Enums, Constants
  Product.Application/      Products/{Commands,Queries,DTOs,Validation,Mapping,Abstractions}, Common/{Behaviors,Results,DTOs,Logging,Exceptions,Abstractions}
  Product.Infrastructure/   Persistence/{Configurations,Migrations,Seed}, Products/ (repositório e queries)
  Product.Api/              Controllers, Authentication, Authorization, ErrorHandling, Extensions, Swagger
UnitTests/Product.Api.UnitTest/   Api, Application, Behaviors, Common, Handlers, Infrastructure, Validators, TestData
scripts/smoke-test.ps1            verificação ponta a ponta
docs/plansAndSpecs/               especificação e planos de implementação anteriores (não fazem parte desta documentação)
```

Ficheiros de raiz relevantes: `StoreHouse.slnx`, `Directory.Build.props` (`net10.0`, C# 14, nullable), `Directory.Packages.props` (gestão central de versões de pacotes), `docker-compose.yml`, `.env.example`.

## 4. Dependências entre projetos

```
Product.Api ──► Product.Application ──► Product.Domain
     │                                       ▲
     └────────► Product.Infrastructure ──────┘
                (também referencia Product.Application)

App.IdentityServer          (sem referências para os projetos Product.*)
Product.Api.UnitTest ──► Product.Api, Product.Application
```

`Product.Api` regista as implementações de `Product.Infrastructure` via `AddInfrastructure`, e `Product.Application` define as interfaces (`IProductRepository`, `IProductQueries`, `IUnitOfWork`).

## 5. Fluxo geral

1. O cliente chama `POST /api/auth/login` no IdentityServer. O `AuthController` faz um pedido *password grant* ao `IdentityServer:TokenEndpoint` e devolve `accessToken`, `tokenType` e `expiresIn`. Credenciais inválidas dão `401`; qualquer outro erro do token endpoint lança exceção (`500`).
2. O cliente envia `Authorization: Bearer <token>` à `Product.Api`. O JwtBearer valida emissor, audiência (`product.api`) e validade, e lê o role do claim `role`.
3. O controller envia um command/query com `ISender`. O pipeline do MediatR executa, por ordem: `LoggingBehavior` → `ValidationBehavior` → handler.
4. O handler devolve um `Result`/`Result<T>`. Os commands usam `IProductRepository` + `IUnitOfWork`; as queries usam `IProductQueries`.
5. `ResultExtensions` converte erros em `ProblemDetails` (`application/problem+json`) com `code` e `traceId`:

| `ErrorType` | HTTP |
|---|---|
| `Validation` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `BusinessRule` | 422 |
| Outros | 500 |

Exceções não tratadas passam pelo `GlobalExceptionHandler` (`500`; o detalhe da exceção só é incluído em `Development`). Respostas `401`, `403` e `404` de rota também usam `ProblemDetails` (`UseStatusCodePages`).

## 6. Modelo de dados (BD `StoreHouse`)

| Tabela | Notas |
|---|---|
| `Product` | `Id` gerado pela sequence `ProductIdSequence` (100000–999999, sem ciclo, início em 100011); preço > 0 (`CHECK`); soft delete (`IsDeleted`, `DeletedAt`, `DeletedBy`); `Version` mapeada como *row version* (`xmin`). |
| `Stock` | 1:1 com `Product` (PK = `ProductId`); `CHECK (Quantity >= 0)`; índice em `Quantity`. |
| `StockMovement` | Histórico de movimentos (`Initial`, `Increase`, `Decrease`) com `QuantityAfter` e `CreatedBy`. |

Os três tipos têm um filtro global que esconde produtos apagados. A migration inicial insere 10 produtos de seed (IDs 100001–100010). A BD do IdentityServer (`StoreHouseIdentity`) usa o esquema ASP.NET Identity, com seed de 2 utilizadores (`Admin`, `Manager`), 2 roles e os respetivos role claims.

## 7. Autorização na Product.Api

| Endpoint | Regra |
|---|---|
| `GET` (lista, por id, `search`, `stock-level`) | Anónimo |
| `POST /api/products`, `DELETE /api/products/{id}` | role `Admin` |
| `PUT /api/products/{id}`, `add-to-stock`, `decrement-stock` | Qualquer utilizador autenticado (a API não verifica o role `Manager` explicitamente) |

## 8. Tecnologias e integrações

| Área | Tecnologia (versão em `Directory.Packages.props`) |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| Base de dados | PostgreSQL (`postgres:18` no Compose), EF Core 10.0.13, Npgsql 10.0.3, extensão `pg_trgm` |
| Identidade | Duende IdentityServer 8.0.10 (+ AspNetIdentity), ASP.NET Identity, Duende.IdentityModel 8.1.0 |
| Autenticação na API | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13 |
| Application | MediatR 12.5.0, FluentValidation 12.2.0, Mapster 10.0.14 |
| Documentação da API | Swashbuckle.AspNetCore 10.3.1 (ativo só em `Development`) |
| Testes | xUnit v3 3.2.2, Moq 4.20.72, `Microsoft.Extensions.TimeProvider.Testing` |
| Contentores | Dockerfiles por serviço e `docker-compose.yml` (Postgres, IdentityServer, Product.Api) |

A única integração entre serviços é a validação de tokens: a `Product.Api` obtém a configuração OIDC do IdentityServer (`Jwt:Authority` / `Jwt:MetadataAddress`). Não há outras integrações externas no código.

## 9. Decisões arquiteturais identificáveis

| Decisão | Implementação |
|---|---|
| CQRS | Commands usam repositório com *tracking*; queries (`ProductQueries`) usam `AsNoTracking` e `ProjectToType<ProductDTO>`. |
| Erros esperados via `Result` | Handlers devolvem `Error` em vez de lançar exceções; a Application não conhece HTTP. |
| IDs de 6 dígitos | Sequence PostgreSQL como valor por omissão da coluna `Id`. |
| Stock concorrente | `ExecuteUpdate` condicional (`Quantity >= @q` / limite superior) dentro de transação com *execution strategy* (`EnableRetryOnFailure(3)`), junto com o registo em `StockMovement`. |
| `PUT`/`DELETE` concorrentes | Concorrência otimista com `xmin` (`Product.Version`), limitada ao intervalo entre a leitura da entidade e o `SaveChanges` dentro do mesmo pedido; `DbUpdateConcurrencyException` → `ConcurrencyConflictException` → `409`. Não deteta dados desatualizados enviados pelo cliente (ver secção 10). |
| Soft delete | Filtro global do EF Core; histórico de stock preservado. |
| Paginação | *Keyset* por `Id` (`lastItemId`, `pageSize` 1–50, por omissão 5); resposta `items`/`nextCursor`/`hasMore`. |
| Pesquisa por nome | `ILIKE` com escape de `%`, `_` e `\` (`LikePatternEscaper`) e índice GIN trigram. |
| `DbContext` pooling | `Product.Infrastructure` usa `AddDbContextPool<ProductDbContext>`, que reutiliza instâncias do contexto entre pedidos. O contexto não guarda estado próprio e o `UnitOfWork` faz `ChangeTracker.Clear()` no início de cada transação. O IdentityServer usa `AddDbContext` (sem pooling). |
| *Connection pooling* | Gerido pelo Npgsql, configurado na connection string: `Pooling=true` e `Maximum Pool Size=50` (também `Minimum Pool Size=0` na `Product.Api`) em `docker-compose.yml` e nos exemplos do README. Não está definido em `appsettings.json`. |
| Migrations no arranque | Ambos os serviços chamam `MigrateAsync()` ao iniciar. |
| Clients e scopes em memória | Definidos em `IdentityServerConfig` a partir de `IdentityServerSettings`; utilizadores na BD. |
| Fluxo OAuth | *Resource owner password* num único client (`storehouse.swagger`, valor por omissão em `appsettings.json`), sem refresh token. |
| Claims | `DistinctClaimsProfileService` remove claims duplicados no token. |
| Tempo | `TimeProvider` injetado nos handlers. |

## 10. Validação, segurança e concorrência

**Validação (MediatR + FluentValidation)**
- Cada command/query tem um `AbstractValidator`, com regras partilhadas em `ProductRules` e limites em `ProductConstraints`.
- O `ValidationBehavior` corre todos os validadores antes do handler, agrupa as mensagens por propriedade e devolve `Error.Validation` (`400`). Se falhar, o handler não é executado.
- Erros de *model binding* seguem o mesmo formato (`InvalidModelStateResponseFactory`).
- A BD repete parte das regras como segunda defesa (`CHECK` em preço, stock e movimentos, e tamanhos máximos de colunas).

**Segurança**

| Risco | Medida no código |
|---|---|
| SQL injection | Não há SQL manual (`FromSql`/`ExecuteSql` não são usados): LINQ e `ExecuteUpdate` do EF Core geram queries parametrizadas. No `ILIKE`, o padrão vai como parâmetro e `%`, `_` e `\` do input são escapados. |
| Input inválido | Validação de IDs, tamanhos, preços, quantidades e paginação (ver acima). |
| Acesso indevido | JWT validado (emissor, audiência, validade, tolerância de 30 s); `Admin` obrigatório em `POST`/`DELETE`; lockout de 5 falhas / 5 min no login; passwords de seed apenas como hash. |
| Fuga de informação | O detalhe de exceções só aparece em `Development`; segredos vêm de configuração (User Secrets / `.env`), não do código. |
| Transporte e contentores | `UseHttpsRedirection` e `UseHsts` (fora de `Development`); imagens executadas com `USER $APP_UID` (sem root). |

**Concorrência (*race conditions*)**

| Cenário | Técnica |
|---|---|
| Dois pedidos geram o mesmo ID | Sequence do PostgreSQL (atómica entre instâncias). |
| Dois pedidos alteram o stock em simultâneo | `UPDATE` único e condicional, sem ler-e-escrever na aplicação: `Quantity >= @q` ao diminuir e limite superior ao aumentar. Se afetar 0 linhas, o handler distingue "não existe" de "stock insuficiente/limite excedido". |
| Stock e histórico inconsistentes | Update e `StockMovement` na mesma transação (rollback se o `Result` falhar). |
| Stock negativo | `CHECK (Quantity >= 0)` como última defesa. |
| `PUT`/`DELETE` em simultâneo sobre o mesmo produto | Concorrência otimista com `xmin` (`Product.Version`). O handler lê o produto (`GetByIdAsync`), altera-o e chama `SaveChanges`; o `UPDATE` inclui `WHERE xmin = <valor lido>`. Se outro pedido gravou o mesmo produto entre a leitura e a gravação, nenhuma linha é afetada, o EF Core lança `DbUpdateConcurrencyException` e a API responde `409` (`Product.ConcurrencyConflict`). |

**Alcance da proteção com `xmin`**

- **Coberto: conflito entre pedidos em processamento.** O valor de `xmin` é lido e comparado no servidor, dentro de um único pedido. A janela de deteção é apenas o intervalo entre a leitura e a gravação desse pedido. Dois `PUT`/`DELETE` que leem o produto antes de qualquer um gravar: o primeiro grava, o segundo recebe `409`.
- **Não coberto: o cliente envia dados que consultou antes e que entretanto ficaram desatualizados.** O `xmin` não faz parte dos DTOs nem das respostas (`Version` não é exposto) e o `PUT` não recebe `If-Match` nem outra versão. Por isso a API não sabe com que versão o cliente trabalhou. Se o cliente fez `GET`, outro pedido já concluído alterou o produto e depois o cliente envia `PUT`, o handler lê o `xmin` atual, o `UPDATE` tem sucesso e a alteração intermédia é sobrescrita (*last write wins*), sem `409`. Detetar este caso exigiria que o cliente devolvesse a versão lida e que o handler a comparasse com a atual.
- **Fora do âmbito do `xmin`: stock.** `add-to-stock` e `decrement-stock` usam `ExecuteUpdate` condicional, que não carrega a entidade nem usa o token de concorrência; são protegidos pelo `UPDATE` condicional e pelo `CHECK` (linhas acima). Embora `Stock` também tenha uma coluna `xmin` mapeada, ela não é verificada por estas operações.
- **`DELETE` repetido:** se o primeiro `DELETE` já foi confirmado, o filtro global de soft delete esconde o produto e o segundo recebe `404`; só recebe `409` se os dois leram o produto antes de qualquer gravação.

Limitação conhecida da repetição automática de operações de stock: ver [Runbook.md](Runbook.md#6-problemas-conhecidos).

## 11. Não existe no código

Health checks, rate limiting, versionamento da API, pipeline de CI, testes de integração e sink de logs configurado (apenas logging por omissão do ASP.NET Core). Os testes unitários não cobrem repositório, queries SQL, `UnitOfWork`, migrations nem `App.IdentityServer`.

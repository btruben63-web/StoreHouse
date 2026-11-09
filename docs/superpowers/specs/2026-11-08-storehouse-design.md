# StoreHouse — Especificação de Design

- **Data:** 2026-11-08
- **Origem:** exercício "Carl Zeiss Vision — .NET development hands-on assessment" + `regras_para_construir_aplicacaov3.txt`
- **Estado:** aprovado em brainstorming, a aguardar revisão final antes do plano de implementação

## 1. Objetivo

Construir a solução `StoreHouse` (.NET 10, C# 14) com:

1. `App.IdentityServer` — emite tokens JWT para os utilizadores `Admin` e `Manager`.
2. `Product.Api` — API REST de gestão de produtos e stock (9 endpoints do enunciado), protegida pelos tokens do ponto 1.
3. `Product.Api.UnitTest` — testes unitários xUnit com mocks.

Cumprir a checklist do enunciado:
- 9 endpoints implementados;
- ID de produto com 6 dígitos, único mesmo com várias instâncias;
- EF Core migrations code-first com seed;
- testes unitários de vários cenários;
- README para correr localmente.

**Fora de âmbito** (decisão explícita): testes de integração, BDD, health checks, rate limiting, Serilog, versionamento da API e CI.

## 2. Tecnologias e versões

| Item | Versão |
|---|---|
| .NET / C# | 10 / 14 |
| PostgreSQL | 18 (local 18.6; `postgres:18` em Docker) |
| Duende IdentityServer (+ `Duende.IdentityServer.AspNetIdentity`) | 8.0.10 |
| Duende.IdentityModel (cliente do token endpoint) | última estável |
| EF Core + `Microsoft.EntityFrameworkCore.Design` / Tools | 10.0.13 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.x |
| Swashbuckle.AspNetCore | 10.3.1 |
| MediatR | 12.5.0 (última versão com licença livre) |
| Mapster (+ Mapster.DependencyInjection) | 10.0.14 |
| FluentValidation (+ DependencyInjectionExtensions) | última estável |
| xUnit v3, Moq, FluentValidation.TestHelper | últimas estáveis (sem FluentAssertions, que passou a ter licença comercial na v8) |

Nota: o Swashbuckle 10 usa o Microsoft.OpenApi 2.x. A configuração do `AddSecurityDefinition` e do `AddSecurityRequirement` segue a API nova, que usa `OpenApiSecuritySchemeReference`.

## 3. Estrutura da solução

```
StoreHouse/
├── StoreHouse.slnx                  (pastas de solução: IdentityServer, Product, UnitTests)
├── IdentityServer/
│   └── App.IdentityServer/          (ASP.NET Core Web API, Dockerfile Linux)
├── Product/
│   ├── Product.Domain/              (entidades anémicas, enums; sem dependências)
│   ├── Product.Application/         (CQRS, MediatR, FluentValidation, Mapster, DTOs, Result)
│   ├── Product.Infrastructure/      (EF Core, Npgsql, DbContext, UnitOfWork, repositórios, Migrations)
│   └── Product.Api/                 (Presentation: Controllers, Swagger, JWT, Global Exception Handler, Dockerfile)
├── UnitTests/
│   └── Product.Api.UnitTest/        (xUnit + Moq)
├── docker-compose.yml
├── .env.example
├── .gitignore                       (inclui .env, bin/, obj/, certificados)
└── README.md
```

Dependências entre projetos:
- `Product.Api → Product.Application, Product.Infrastructure`
- `Product.Infrastructure → Product.Application, Product.Domain`
- `Product.Application → Product.Domain`
- `Product.Domain →` nenhuma
- `Product.Api.UnitTest → Product.Api, Product.Application`

Opções dos projetos Web API (iguais às do template da imagem `webapp.png`):
- .NET 10;
- Authentication: None (o JWT é configurado à mão);
- HTTPS ativo;
- container support Linux com Dockerfile;
- OpenAPI ativo;
- top-level statements;
- controllers;
- sem Aspire.

As duas APIs arrancam no Swagger (`launchUrl: swagger`).

## 4. App.IdentityServer

### 4.1 Base de dados `StoreHouseIdentity`

- `IdentityDbContext<IdentityUser>` com Npgsql. As migrations criam as tabelas padrão do ASP.NET Identity.
- Seed com `HasData`:
  - **Roles:** `Admin` e `Manager`, com IDs e `ConcurrencyStamp` fixos.
  - **Utilizadores:**

    | UserName | Password (só a hash fica no código) | Role | Restantes dados |
    |---|---|---|---|
    | `Admin` | `nebur#123` | Admin | fictícios (email `admin@storehouse.local`, telefone, `EmailConfirmed = true`) |
    | `Manager` | `nebur2#123` | Manager | fictícios (email `manager@storehouse.local`, …) |

    - `PasswordHash` é pré-calculado com o `PasswordHasher<IdentityUser>` e guardado como constante.
    - `SecurityStamp` e `ConcurrencyStamp` são fixos, para que novas migrations não gerem alterações falsas.
    - `NormalizedUserName` e `NormalizedEmail` são preenchidos.
  - **`AspNetUserRoles`:** associa cada utilizador à sua role.
  - **`AspNetRoleClaims`:** `JwtClaimTypes.Role` = `Admin` para a role Admin, e `Manager` para a role Manager.
- As passwords em texto nunca aparecem no código (só no README, como dados de demonstração — ver secção 8).

### 4.2 Configuração do Duende

- `AddIdentityServer(o => o.IssuerUri = config["IdentityServer:IssuerUri"])` + `.AddAspNetIdentity<IdentityUser>()`.
  - O `IssuerUri` é fixo para o `iss` ser estável entre local e Docker.
- Recursos em memória, lidos do `appsettings.json`:
  - `ApiScope("product.api")`;
  - `ApiResource("product.api")` com `UserClaims = { "role" }`;
  - `IdentityResources.OpenId`.
- Client `storehouse.swagger`:
  - `AllowedGrantTypes = ResourceOwnerPassword`;
  - `AllowedScopes = { "product.api" }`;
  - `AccessTokenLifetime = 3600`.
  - O `ClientSecret` vem de User Secrets em desenvolvimento e de uma variável de ambiente no Docker. Nunca fica em `appsettings.json`.
- O perfil de serviço inclui a claim `role` no access token, através das role claims do ASP.NET Identity.
- Licença: o Duende funciona sem licença em desenvolvimento e testes (apenas com avisos no log). Isto fica documentado no README.

### 4.3 Endpoint

`AuthController` — `POST /api/auth/login`

- **Pedido:** `LoginRequestDTO(string UserName, string Password)`, com `[Required]` e tamanho máximo.
- **Funcionamento:** usa um `HttpClient` com o `RequestPasswordTokenAsync` do Duende.IdentityModel contra o próprio endpoint `/connect/token`.
- **Respostas:**
  - **200:** `TokenResponseDTO(string AccessToken, string TokenType, int ExpiresIn)`;
  - **401:** credenciais inválidas, devolvidas em `ProblemDetails` sem revelar se o utilizador existe;
  - **400:** pedido inválido.

O Swagger do IdentityServer mostra apenas este endpoint. Os endpoints do protocolo OIDC não aparecem no Swagger.

### 4.4 Arranque

- `Database.MigrateAsync()` no arranque.
- O EF Core 9+ usa um lock de migração na base de dados, por isso várias instâncias a arrancar ao mesmo tempo não aplicam a mesma migration duas vezes.

## 5. Product.Api

### 5.1 Base de dados `StoreHouse`

Os nomes de tabelas, colunas e índices ficam em inglês e em PascalCase (o padrão do EF Core com Npgsql).

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE SEQUENCE "ProductIdSequence"
    START 100011 INCREMENT 1 MINVALUE 100000 MAXVALUE 999999 NO CYCLE;
```

A sequence começa em 100011 porque o seed ocupa os IDs 100001 a 100010.

**Product**

| Coluna | Tipo | Regras |
|---|---|---|
| Id | integer | PK, `DEFAULT nextval('"ProductIdSequence"')` |
| Name | varchar(100) | NOT NULL |
| Description | varchar(500) | NULL |
| Category | varchar(50) | NOT NULL |
| Price | numeric(10,2) | NOT NULL, `CHECK (Price > 0)` |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NULL |
| IsDeleted | boolean | NOT NULL DEFAULT false |
| DeletedAt | timestamptz | NULL |
| DeletedBy | varchar(100) | NULL |
| xmin | xid | concurrency token (`IsRowVersion()`) |

Índices:
- `IX_Product_Name_Trgm` — GIN (`Name gin_trgm_ops`) `WHERE "IsDeleted" = false`;
- `IX_Product_Category` — (`Category`) `WHERE "IsDeleted" = false`.

**Stock** (relação 1:1 com Product)

| Coluna | Tipo | Regras |
|---|---|---|
| ProductId | integer | PK + FK → `Product.Id` `ON DELETE RESTRICT` |
| Quantity | integer | NOT NULL, `CHECK (Quantity >= 0)` |
| UpdatedAt | timestamptz | NOT NULL |
| xmin | xid | concurrency token |

Índices: `IX_Stock_Quantity` (`Quantity`).

**StockMovement**

| Coluna | Tipo | Regras |
|---|---|---|
| Id | bigint | PK, `GENERATED ALWAYS AS IDENTITY` |
| ProductId | integer | FK → `Product.Id` `ON DELETE RESTRICT` |
| Type | smallint | NOT NULL, `CHECK (Type IN (1,2,3))` — 1 Initial, 2 Increase, 3 Decrease |
| Quantity | integer | NOT NULL, `CHECK (Quantity > 0)` |
| QuantityAfter | integer | NOT NULL |
| CreatedAt | timestamptz | NOT NULL |
| CreatedBy | varchar(100) | NULL (username do token; `seed` no seed) |

Índices: `IX_StockMovement_ProductId_CreatedAt` (`ProductId`, `CreatedAt DESC`).

Nota: um movimento `Initial` com stock inicial 0 não é criado, para respeitar o `CHECK (Quantity > 0)`.

**Seed com `HasData`:** 10 produtos com dados em português, o respetivo `Stock` e um movimento `Initial` para cada um. Datas fixas.

| Id | Name | Category | Price | Stock |
|---|---|---|---|---|
| 100001 | Martelo de Carpinteiro | Ferramentas Manuais | 12.90 | 40 |
| 100002 | Berbequim sem Fios 18V | Ferramentas Elétricas | 89.99 | 15 |
| 100003 | Fita Métrica 5m | Medição | 6.50 | 120 |
| 100004 | Alicate Universal | Ferramentas Manuais | 9.75 | 60 |
| 100005 | Serrote de Costas | Ferramentas Manuais | 14.20 | 25 |
| 100006 | Nível de Bolha 60cm | Medição | 11.40 | 30 |
| 100007 | Jogo de Chaves de Fendas | Ferramentas Manuais | 18.90 | 45 |
| 100008 | Caixa de Parafusos 200un | Fixação | 7.30 | 200 |
| 100009 | Lixa para Madeira P120 | Consumíveis | 1.20 | 500 |
| 100010 | Cola de Madeira 750ml | Consumíveis | 8.60 | 80 |

**Soft delete:**
- O `HasQueryFilter(p => !p.IsDeleted)` é aplicado no `ProductDbContext`.
- As FKs são `RESTRICT`, porque nunca há apagamento físico.
- Os IDs de produtos apagados nunca são reutilizados.

### 5.2 Domain (modelo anémico)

- `Product`, `Stock`, `StockMovement`: apenas propriedades e navegações (`Product.Stock`, `Product.StockMovements`).
- `enum StockMovementType : short { Initial = 1, Increase = 2, Decrease = 3 }`.

### 5.3 Application

**Organização por feature:**
- `Products/Commands/<Operacao>/{Command, CommandHandler, CommandValidator}`;
- `Products/Queries/<Operacao>/{Query, QueryHandler, QueryValidator}`.

**Operações (nomes dos métodos e dos pedidos MediatR):**

| Tipo | Request | Resposta |
|---|---|---|
| Query | `GetProductsQuery(int? LastItemId, int PageSize = 5)` | `Result<PagedResultDTO<ProductDTO>>` |
| Query | `GetProductByIdQuery(int Id)` | `Result<ProductDTO>` |
| Query | `SearchProductsByNameQuery(string Name, int? LastItemId, int PageSize = 5)` | `Result<PagedResultDTO<ProductDTO>>` |
| Query | `SearchProductsByStockRangeQuery(int Min, int Max, int? LastItemId, int PageSize = 5)` | `Result<PagedResultDTO<ProductDTO>>` |
| Command | `CreateProductCommand(CreateProductDTO Product, string? UserName)` | `Result<ProductDTO>` |
| Command | `UpdateProductCommand(int Id, UpdateProductDTO Product)` | `Result<ProductDTO>` |
| Command | `DeleteProductCommand(int Id, string? UserName)` | `Result` |
| Command | `AddStockToProductCommand(int Id, int Quantity, string? UserName)` | `Result<StockDTO>` |
| Command | `DecreaseStockToProductCommand(int Id, int Quantity, string? UserName)` | `Result<StockDTO>` |

**DTOs (todos `record`):**
- `ProductDTO(int Id, string Name, string? Description, string Category, decimal Price, int StockQuantity)`;
- `CreateProductDTO(string Name, string? Description, string Category, decimal Price, int InitialStock)`;
- `UpdateProductDTO(string Name, string? Description, string Category, decimal Price)` — o stock só se altera pelos endpoints de stock;
- `StockDTO(int ProductId, int Quantity)`;
- `PagedResultDTO<T>(IReadOnlyList<T> Items, int? NextCursor, bool HasMore)`.

Os DTOs de entrada têm DataAnnotations (`[Required]`, `[StringLength]`, `[Range]`), para o `[ApiController]` devolver 400 logo à entrada.

**Abstrações (implementadas na Infrastructure):**

`IProductRepository` (escrita, com tracking):
- `GetByIdAsync`;
- `Add`;
- `ExistsAsync` (usa `AnyAsync`);
- `TryAddStockAsync` / `TryDecreaseStockAsync`: fazem um `ExecuteUpdateAsync` e devolvem o número de linhas afetadas;
- `GetStockQuantityAsync`;
- `AddMovement`.

`IProductQueries` (leitura):
- usa sempre `AsNoTracking()`, projeção Mapster `ProjectToType<ProductDTO>()` e paginação keyset (`WHERE Id > @lastItemId ORDER BY Id LIMIT pageSize + 1`, para calcular o `HasMore`).

`IUnitOfWork`:
- `SaveChangesAsync`;
- `ExecuteInTransactionAsync(Func<CancellationToken, Task<Result<T>>>)`, que envolve a transação na execution strategy do Npgsql (`EnableRetryOnFailure`).

`IDateTimeProvider` (`TimeProvider`), para os testes conseguirem controlar as datas.

**Padrão Result:**
- `Result` / `Result<T>` com `Error(string Code, string Message, ErrorType Type)`.
- `ErrorType` pode ser `Validation`, `NotFound`, `Conflict` ou `BusinessRule`. Só é usado para resultados esperados.
- Erros conhecidos centralizados em `ProductErrors`, por exemplo:
  - `ProductErrors.NotFound(id)`;
  - `ProductErrors.InsufficientStock(id, available, requested)`;
  - `ProductErrors.ConcurrencyConflict(id)`;
  - `ProductErrors.StockLimitExceeded(id)`.

**Pipeline do MediatR:**
1. `LoggingBehavior<TRequest,TResponse>`: regista o nome do pedido e a duração com `ILogger<T>` e `LoggerMessage` source-generated. Se o resultado for uma falha, regista um warning com o código do erro.
2. `ValidationBehavior<TRequest,TResponse>`: corre todos os `IValidator<TRequest>`. Se houver erros, devolve um `Result` de falha do tipo `Validation` com os detalhes, sem lançar exceção e sem chamar o handler. A restrição `TResponse : IResultBase` (interface própria, para não colidir com o `Microsoft.AspNetCore.Http.IResult`) cria a falha através de um método estático abstrato da interface.

**Validações (FluentValidation):**

| Campo | Regra |
|---|---|
| Name | obrigatório, não vazio/espaços, ≤ 100 |
| Category | obrigatória, ≤ 50 |
| Description | ≤ 500 |
| Price | > 0, ≤ 99 999 999.99, máx. 2 casas decimais |
| InitialStock | 0 – 1 000 000 |
| Id | 100000 – 999999 |
| Quantity (stock) | 1 – 1 000 000 |
| Min / Max | Min ≥ 0, Max ≥ Min |
| Name (search) | 2 – 100 caracteres |
| PageSize | 1 – 50 (default 5) |
| LastItemId | nulo ou 100000 – 999999 |

**Mapster:** um `ProductMappingConfig : IRegister` define `StockQuantity = src.Stock.Quantity`. A mesma configuração serve para os mapeamentos em memória e para o `ProjectToType`.

**Logging nos handlers:** `ILogger<THandler>` regista as operações de negócio (produto criado, apagado, stock alterado com a quantidade anterior e a nova, stock insuficiente) com mensagens estruturadas.

### 5.4 Infrastructure

**Registo do `ProductDbContext`:**
- `AddDbContextPool<ProductDbContext>`;
- connection string Npgsql com `Pooling=true;Maximum Pool Size=…`;
- `EnableRetryOnFailure`.

**Configuração das entidades:**
- uma classe `IEntityTypeConfiguration<T>` por entidade;
- os índices, `CHECK`s, a sequence, o `xmin` e o seed ficam na configuração.

**Migrations:** projeto `Product.Infrastructure`, com o `Product.Api` como startup project.

**Concorrência no stock (race conditions):**
- **Increase:**
  - `UPDATE "Stock" SET "Quantity" = "Quantity" + @q, "UpdatedAt" = @now WHERE "ProductId" = @id AND <produto não apagado>`, através de `ExecuteUpdateAsync`.
  - Corre dentro de `ExecuteInTransactionAsync`.
  - Depois do `UPDATE`, lê o `QuantityAfter` (a linha continua bloqueada até ao commit) e insere o `StockMovement`.
  - Por fim, faz o commit.
- **Decrease:** igual ao increase, mas com a condição extra `AND "Quantity" >= @q`.
  - Se o `UPDATE` não afetar nenhuma linha, o `ExistsAsync` decide a resposta: `NotFound` se o produto não existir, `InsufficientStock` (BusinessRule) se existir.
  - O `CHECK (Quantity >= 0)` é a última defesa.
- **Overflow:** a quantidade está limitada a 1 000 000 por pedido. O increase acrescenta a condição `AND "Quantity" <= 2147483647 - @q`, para a própria soma nunca transbordar. Se essa condição falhar, a resposta é `BusinessRule` (`ProductErrors.StockLimitExceeded`).

**Concorrência no PUT:**
- Usa o `xmin` como concurrency token.
- Um `DbUpdateConcurrencyException` é apanhado apenas no `UpdateProductCommandHandler` e convertido em `ProductErrors.ConcurrencyConflict`, o que dá um 409.

**Create:**
- Uma única transação insere o `Product`, o `Stock` e o movimento `Initial` (quando o stock inicial é maior que 0), com um só `SaveChangesAsync`.
- O `Id` vem da sequence.

**SQL injection:**
- Só se usa LINQ e `ExecuteUpdateAsync`, que são parametrizados. Nunca se usa `FromSqlRaw` com concatenação.
- A pesquisa usa `EF.Functions.ILike(p.Name, $"%{Escape(name)}%")`, com escape de `\`, `%` e `_`.

### 5.5 Api (Presentation)

**Endpoints (`ProductsController`, `[ApiController]`, `[Route("api/products")]`):**

| Ação | HTTP | Autorização | Sucesso | Erros |
|---|---|---|---|---|
| `GetProducts` | `GET /api/products?lastItemId&pageSize` | pública | 200 | 400 |
| `GetProductById` | `GET /api/products/{id:int}` | pública | 200 | 400, 404 |
| `CreateProduct` | `POST /api/products` | `[Authorize(Roles = "Admin")]` | 201 + Location | 400, 401, 403 |
| `UpdateProduct` | `PUT /api/products/{id:int}` | `[Authorize]` | 200 | 400, 401, 404, 409 |
| `DeleteProduct` | `DELETE /api/products/{id:int}` | `[Authorize(Roles = "Admin")]` | 204 | 400, 401, 403, 404 |
| `DecreaseStockToProduct` | `POST /api/products/{id:int}/decrement-stock/{quantity:int}` | `[Authorize]` | 200 | 400, 401, 404, 422 |
| `AddStockToProduct` | `POST /api/products/{id:int}/add-to-stock/{quantity:int}` | `[Authorize]` | 200 | 400, 401, 404, 422 |
| `SearchProductsByName` | `GET /api/products/search?name&lastItemId&pageSize` | pública | 200 | 400 |
| `SearchProductsByStockRange` | `GET /api/products/stock-level?min&max&lastItemId&pageSize` | pública | 200 | 400 |

**Regras dos controllers:**
- O controller só envia pedidos ao `ISender` do MediatR e converte o `Result` com a extensão `ToActionResult()`, usando o `ProblemDetailsFactory`.
- Conversão de `ErrorType` para código HTTP:
  - `Validation` → 400, em `ValidationProblemDetails` com os erros por campo;
  - `NotFound` → 404;
  - `Conflict` → 409;
  - `BusinessRule` → 422.
- O `UserName` é lido do `User` (claim `name` / `sub`) no controller e passado ao command. A Application não conhece o `HttpContext`.
- Todas as ações são `async` e recebem um `CancellationToken`.
- Os atributos `[ProducesResponseType]` documentam as respostas no Swagger.

**Autenticação:**
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`, com:
  - `Authority`;
  - `MetadataAddress` (endereço interno no Docker);
  - `ValidIssuer` = `IssuerUri`;
  - `ValidAudience = "product.api"`;
  - `MapInboundClaims = false`;
  - `RoleClaimType = "role"`;
  - `NameClaimType = "name"`.
- Tudo vem da secção `Jwt` da configuração.

**Swagger:**
- `AddSecurityDefinition("Bearer", Http/bearer/JWT)`.
- Requisito de segurança aplicado só às ações com `[Authorize]`, através de um `IOperationFilter`. Por isso, o cadeado só aparece nos POST, PUT e DELETE.

**Erros:**
- `GlobalExceptionHandler : IExceptionHandler`, com `AddProblemDetails()` e `UseExceptionHandler()`.
- Faz log do erro e devolve um 500 em `ProblemDetails` com o `traceId`, sem stack trace fora do ambiente de desenvolvimento.
- Não há try/catch para logging nos restantes métodos.

**Segurança:**
- `UseHttpsRedirection`;
- HSTS fora do ambiente de desenvolvimento;
- secrets em User Secrets ou variáveis de ambiente;
- containers com o utilizador `app`, sem privilégios de administrador.

**Arranque:** `Database.MigrateAsync()`, com o lock de migração do EF Core.

## 6. Docker

**Dockerfiles multi-stage** (`IdentityServer/App.IdentityServer/Dockerfile`, `Product/Product.Api/Dockerfile`):
- `sdk:10.0` faz o restore (copiando primeiro só os `.csproj`, para cache), o build e o publish;
- `aspnet:10.0` corre a aplicação com `USER app`;
- expõe as portas 8080 e 8081.
- O build context é sempre a raiz `StoreHouse/`.

**docker-compose.yml:**

| Serviço | Imagem / build | Portas | Depende de |
|---|---|---|---|
| `postgres` | `postgres:18`, volume `pgdata`, healthcheck `pg_isready` | 5432 | — |
| `app-identityserver` | `context: .`, `IdentityServer/App.IdentityServer/Dockerfile` | `7001:8081` | postgres (healthy) |
| `product-api` | `context: .`, `Product/Product.Api/Dockerfile` | `7039:8081` | postgres (healthy), app-identityserver |

- **HTTPS:** o certificado de desenvolvimento é exportado com `dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p <pwd>` e montado como volume em leitura. Usa as variáveis `ASPNETCORE_Kestrel__Certificates__Default__Path` e `__Password`.
- **Issuer:**
  - `IdentityServer__IssuerUri = https://localhost:7001`;
  - na Product.Api, `Jwt__MetadataAddress = http://app-identityserver:8080/.well-known/openid-configuration` e `Jwt__RequireHttpsMetadata = false`, só no compose.
- **`.env.example`:**
  - `POSTGRES_USER`, `POSTGRES_PASSWORD`;
  - `IDENTITY_CLIENT_SECRET`;
  - `CERT_PASSWORD`;
  - connection strings derivadas.

## 7. Testes — Product.Api.UnitTest

Stack: xUnit v3, Moq e `FluentValidation.TestHelper`.

| Área | Cenários |
|---|---|
| Controllers (mock de `ISender`) | os 9 endpoints: caminho feliz (200/201/204, `CreatedAtAction` com Location) e cada `ErrorType` → 400/404/409/422 |
| Autorização (reflexão) | POST create e DELETE com `Roles = "Admin"`; PUT e endpoints de stock com `[Authorize]` sem roles; os GET sem `[Authorize]` |
| Handlers de commands (mocks de `IProductRepository` e `IUnitOfWork`) | Create com stock inicial (o movimento `Initial` é criado ou não); Update inexistente → NotFound; Update com concorrência → Conflict; Delete faz soft delete (IsDeleted, DeletedAt, DeletedBy); Delete repetido → NotFound; Add/Decrease com sucesso (quantidade final e movimento); Decrease com stock insuficiente → BusinessRule; Decrease inexistente → NotFound |
| Handlers de queries (mock de `IProductQueries`) | GetById encontrado ou não; paginação (`HasMore`, `NextCursor`); pesquisas que delegam os parâmetros corretos |
| Validadores | os limites de cada regra da tabela da secção 5.3 |
| Behaviors | o `ValidationBehavior` não chama o handler quando há erros; o `LoggingBehavior` passa o resultado sem o alterar |

## 8. README.md

- Pré-requisitos: .NET 10 SDK, PostgreSQL 18 ou Docker Desktop, Visual Studio 2026.
- **Correr localmente:**
  - `dotnet user-secrets set` para as connection strings e o client secret;
  - `dotnet ef database update` para as duas bases (ou deixar a migração automática do arranque);
  - arrancar os dois projetos.
- **Correr em Docker:**
  - copiar o `.env.example` para `.env`;
  - exportar o certificado;
  - correr `docker compose up --build`.
- **Utilizadores de teste** (dados de demonstração): `Admin` / `nebur#123` (role Admin) e `Manager` / `nebur2#123` (role Manager).
- **Obter o token:** usar o `POST /api/auth/login` no Swagger do IdentityServer, depois o botão Authorize no Swagger da Product.Api com o valor do `AccessToken`.
- Tabela de endpoints e permissões.
- **Decisões de arquitetura:**
  - sequence para os IDs, segura com várias instâncias;
  - `UPDATE` condicional atómico no stock;
  - `xmin` no PUT;
  - soft delete;
  - Result vs exceções;
  - keyset pagination;
  - `pg_trgm`;
  - licença do Duende e MediatR 12.5 (licença livre).
- Como correr os testes: `dotnet test`.

## 9. Riscos e notas

- **Duende IdentityServer:** precisa de licença em produção, e o grant password está desaconselhado no OAuth 2.1. É aceitável aqui por ser um requisito do exercício. Fica documentado.
- **Hash das passwords no código:** a hash fica na configuração do seed, por decisão explícita, e as passwords em texto ficam no README como dados de demonstração.
- **Versões:** se alguma versão pedida não existir ou não for compatível com o .NET 10 na altura da implementação, usa-se a estável mais próxima e regista-se no README.

## 10. Ajustes técnicos feitos na fase de plano (2026-11-09)

Estes ajustes resultam da verificação das APIs reais dos pacotes. Prevalecem sobre o texto anterior deste documento.

1. **Alias `ProductEntity`:** dentro de namespaces `Product.*`, o nome `Product` é resolvido para o namespace e não para a classe. Por isso, a entidade continua a chamar-se `Product` no Domain (tabela `Product`), mas os outros projetos usam `global using ProductEntity = Product.Domain.Entities.Product;`.
2. **`StockMovement.Id`:** passa a `GENERATED BY DEFAULT AS IDENTITY` com `START 11`. O `ALWAYS` impede o seed com IDs explícitos.
3. **Padrão Result:** a interface é `IResultBase<TSelf>`, com `static abstract TSelf Failure(Error)`. Assim, o `ValidationBehavior` consegue criar a falha do tipo certo.
4. **Mapster:** não é preciso o `Mapster.DependencyInjection`. O `TypeAdapterConfig` e o `MapsterMapper.Mapper` do pacote base chegam.
5. **Datas:** usa-se o `TimeProvider` do .NET diretamente, em vez de uma interface própria `IDateTimeProvider`. Nos testes usa-se o `FakeTimeProvider`.
6. **`AddInfrastructure(string connectionString)`:** a Infrastructure não depende de `IConfiguration`, e é a Api que lê a connection string.
7. **Erros HTTP:** os `ProblemDetails` são construídos na `ResultExtensions`, sem `ProblemDetailsFactory`, para os controllers serem testáveis sem `HttpContext` completo.
8. **IdentityServer sem `UseHttpsRedirection`:** no Docker, a Product.Api lê o discovery por HTTP interno (`http://app-identityserver:8080`). O redirecionamento para HTTPS partiria essa chamada. Os `IdentityResources` também não são necessários.
9. **Gestão central de pacotes:** `Directory.Packages.props`, com pinning de dependências transitivas, e `Directory.Build.props` com `net10.0`, C# 14, `Nullable` e `ImplicitUsings`.
10. **Versões de teste:** xUnit v3 3.2.2 (a 4.x é uma major recente), xunit.runner.visualstudio 3.1.5 e Moq 4.20.72.
11. **PostgreSQL no Docker:** porta `5433` no host, para não colidir com o PostgreSQL 18.6 local na `5432`. O volume fica em `/var/lib/postgresql`, que é a localização exigida pela imagem 18.
12. **Script de verificação:** `scripts/smoke-test.ps1` testa os 9 endpoints, a autenticação e as permissões, tanto local como em Docker. As passwords são passadas como parâmetros.
13. **Sem git:** o repositório é criado mais tarde pelo utilizador. Os passos de commit do plano são substituídos por checkpoints (build e testes).

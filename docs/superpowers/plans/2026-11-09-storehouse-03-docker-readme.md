# StoreHouse — Plano 03: docker-compose e README

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pôr a solução completa a correr com um único `docker compose up` (PostgreSQL 18, App.IdentityServer e Product.Api) e entregar o `README.md` com os pré-requisitos e os passos para correr o projeto localmente e em Docker.

**Architecture:** Um `docker-compose.yml` na raiz com 3 serviços.
- **Segredos:** vêm de um `.env`, que fica fora do git; o `.env.example` serve de modelo.
- **HTTPS:** usa o certificado de desenvolvimento do .NET, montado só para leitura.
- **Comunicação entre serviços:** a Product.Api valida os tokens lendo o discovery do IdentityServer pela rede interna em HTTP. O `iss` fica fixo em `https://localhost:7001`.

**Tech Stack:** Docker 29, Docker Compose v2, `postgres:18`, `mcr.microsoft.com/dotnet/aspnet:10.0` e `sdk:10.0`.

**Spec:** `docs/superpowers/specs/2026-11-08-storehouse-design.md` (secções 6, 8 e 10).

**Pré-requisitos:** Planos 01 e 02 concluídos. O `scripts/smoke-test.ps1` já passa localmente.

## Global Constraints

- Raiz: `C:\Users\Ruben Batista\OneDrive\Documents\Projetos\StoreHouse` (`StoreHouse/`).
- **Sem git:** os passos de commit são checkpoints.
- **Portas no host:**
  - App.IdentityServer em `https://localhost:7001`;
  - Product.Api em `https://localhost:7039`;
  - PostgreSQL do compose em `localhost:5433`, para não colidir com o PostgreSQL 18.6 local na `5432`.
- O volume do `postgres:18` é montado em `/var/lib/postgresql`, a localização exigida pela imagem 18.
- `IssuerUri` e `Jwt:Issuer` = `https://localhost:7001` em todos os ambientes.
- O README é escrito em português. Comandos, nomes de ficheiros e configuração ficam em inglês.
- As credenciais dos utilizadores de teste só aparecem no README, como dados de demonstração. O resto dos segredos fica no `.env` (fora do git) ou em User Secrets.

## Mapa de ficheiros

```
StoreHouse/
├── docker-compose.yml        (Task 1)
├── .env.example              (Task 1)
├── .env                      (Task 1, local, ignorado pelo git)
├── certs/aspnetapp.pfx       (Task 1, local, ignorado pelo git)
├── StoreHouse.slnx           (Task 2, acrescenta "Solution Items")
└── README.md                 (Task 2)
```

---

### Task 1: docker-compose com PostgreSQL, App.IdentityServer e Product.Api

Entregável: `docker compose up --build -d` põe os 3 serviços a correr, e o `scripts/smoke-test.ps1` passa contra os containers.

**Files:**
- Create: `docker-compose.yml`, `.env.example`
- Create (local, não versionado): `.env`, `certs/aspnetapp.pfx`

**Interfaces:**
- Consumes:
  - os Dockerfiles do Plano 01 (`IdentityServer/App.IdentityServer/Dockerfile`) e do Plano 02 (`Product/Product.Api/Dockerfile`);
  - as chaves de configuração `ConnectionStrings:StoreHouseIdentity`, `ConnectionStrings:StoreHouse`, `IdentityServer:*` e `Jwt:*`.
- Produces: os serviços `postgres`, `app-identityserver` e `product-api`, e os volumes `pgdata` e `identity-keys`.

- [ ] **Step 1: Criar `.env.example`**

```dotenv
# Copiar para .env e substituir os valores. O .env não deve ir para o git.
POSTGRES_USER=storehouse
POSTGRES_PASSWORD=change-me-postgres
IDENTITY_CLIENT_SECRET=change-me-to-a-long-random-secret
CERT_PASSWORD=change-me-cert
```

- [ ] **Step 2: Criar `docker-compose.yml`**

```yaml
name: storehouse

services:
  postgres:
    image: postgres:18
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5433:5432"
    volumes:
      - pgdata:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d postgres"]
      interval: 5s
      timeout: 5s
      retries: 12

  app-identityserver:
    build:
      context: .
      dockerfile: IdentityServer/App.IdentityServer/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: "8080"
      ASPNETCORE_HTTPS_PORTS: "8081"
      ASPNETCORE_Kestrel__Certificates__Default__Path: /https/aspnetapp.pfx
      ASPNETCORE_Kestrel__Certificates__Default__Password: ${CERT_PASSWORD}
      ConnectionStrings__StoreHouseIdentity: Host=postgres;Port=5432;Database=StoreHouseIdentity;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Pooling=true;Maximum Pool Size=50
      IdentityServer__IssuerUri: https://localhost:7001
      IdentityServer__TokenEndpoint: http://localhost:8080/connect/token
      IdentityServer__ClientSecret: ${IDENTITY_CLIENT_SECRET}
      IdentityServer__KeyPath: /home/app/keys
    ports:
      - "7001:8081"
    volumes:
      - ./certs:/https:ro
      - identity-keys:/home/app/keys
    depends_on:
      postgres:
        condition: service_healthy

  product-api:
    build:
      context: .
      dockerfile: Product/Product.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: "8080"
      ASPNETCORE_HTTPS_PORTS: "8081"
      ASPNETCORE_Kestrel__Certificates__Default__Path: /https/aspnetapp.pfx
      ASPNETCORE_Kestrel__Certificates__Default__Password: ${CERT_PASSWORD}
      ConnectionStrings__StoreHouse: Host=postgres;Port=5432;Database=StoreHouse;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50
      Jwt__Authority: http://app-identityserver:8080
      Jwt__MetadataAddress: http://app-identityserver:8080/.well-known/openid-configuration
      Jwt__Issuer: https://localhost:7001
      Jwt__Audience: product.api
      Jwt__RequireHttpsMetadata: "false"
    ports:
      - "7039:8081"
    volumes:
      - ./certs:/https:ro
    depends_on:
      postgres:
        condition: service_healthy
      app-identityserver:
        condition: service_started

volumes:
  pgdata:
  identity-keys:
```

- [ ] **Step 3: Criar o `.env` local**

Run:
```powershell
Copy-Item .env.example .env
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
(Get-Content .env) `
    -replace 'change-me-postgres', 'StoreHouse!Pg2026' `
    -replace 'change-me-to-a-long-random-secret', $secret `
    -replace 'change-me-cert', 'StoreHouse!Cert2026' | Set-Content .env
Get-Content .env
```
Expected: o `.env` contém os 4 valores preenchidos, sem nenhum `change-me`.

- [ ] **Step 4: Exportar e confiar no certificado HTTPS de desenvolvimento**

Run:
```powershell
New-Item -ItemType Directory -Force certs | Out-Null
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p 'StoreHouse!Cert2026'
dotnet dev-certs https --trust
Get-ChildItem certs
```
Expected: `certs/aspnetapp.pfx` existe. A password tem de ser igual ao `CERT_PASSWORD` do `.env`.

- [ ] **Step 5: Validar o ficheiro compose**

Run: `docker compose config --quiet`
Expected: sem output e com exit code 0. Se houver erros de interpolação, faltam variáveis no `.env`.

- [ ] **Step 6: Parar os serviços locais e arrancar os containers**

Parar qualquer `dotnet run` do App.IdentityServer ou da Product.Api, porque as portas 7001 e 7039 ficam ocupadas pelos containers.

Run:
```powershell
docker compose up --build -d
docker compose ps
```
Expected:
- `postgres` está `healthy`;
- `app-identityserver` e `product-api` estão `running`.

- [ ] **Step 7: Confirmar nos logs que as migrations foram aplicadas**

Run:
```powershell
docker compose logs app-identityserver --tail 50
docker compose logs product-api --tail 50
```
Expected:
- os dois logs mostram `Applying migration '..._InitialIdentity'` ou `'..._InitialCreate'` e depois `Now listening on: https://[::]:8081`;
- não há exceções;
- os avisos de licença do Duende são esperados.

- [ ] **Step 8: Verificar o discovery visto pela Product.Api**

Run: `Invoke-RestMethod https://localhost:7001/.well-known/openid-configuration | Select-Object issuer, jwks_uri`
Expected: `issuer` = `https://localhost:7001`.

- [ ] **Step 9: Correr o smoke test contra os containers**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword 'nebur#123' -ManagerPassword 'nebur2#123'
```
Expected: todas as linhas `OK`, terminando em `Smoke test concluído com sucesso.`

Se o `POST com Admin` devolver `401`, ver o `docker compose logs product-api`. Uma mensagem `IDX10205` (issuer inválido) indica que o `IdentityServer__IssuerUri` e o `Jwt__Issuer` não coincidem. Uma mensagem `IDX20803` indica que o `Jwt__MetadataAddress` não está acessível.

- [ ] **Step 10: Checkpoint**

Run: `docker compose down`
Expected: os containers são removidos. Os volumes `pgdata` e `identity-keys` mantêm-se; `docker compose down -v` apaga-os se for preciso recomeçar do zero.

---

### Task 2: README.md e Solution Items

Entregável: o `README.md` explica como correr o projeto localmente e em Docker, como obter o token e as decisões de arquitetura. Os ficheiros da raiz aparecem no Visual Studio em "Solution Items".

**Files:**
- Create: `README.md`
- Modify: `StoreHouse.slnx` (substituir na totalidade)

**Interfaces:**
- Consumes: as portas, os comandos e os nomes dos Planos 01, 02 e Task 1.
- Produces: a documentação pedida na checklist do enunciado.

- [ ] **Step 1: Substituir `StoreHouse.slnx`**

```xml
<Solution>
  <Folder Name="/IdentityServer/">
    <Project Path="IdentityServer/App.IdentityServer/App.IdentityServer.csproj" />
  </Folder>
  <Folder Name="/Product/">
    <Project Path="Product/Product.Api/Product.Api.csproj" />
    <Project Path="Product/Product.Application/Product.Application.csproj" />
    <Project Path="Product/Product.Domain/Product.Domain.csproj" />
    <Project Path="Product/Product.Infrastructure/Product.Infrastructure.csproj" />
  </Folder>
  <Folder Name="/Solution Items/">
    <File Path=".dockerignore" />
    <File Path=".env.example" />
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="docker-compose.yml" />
    <File Path="README.md" />
    <File Path="scripts/smoke-test.ps1" />
  </Folder>
  <Folder Name="/UnitTests/">
    <Project Path="UnitTests/Product.Api.UnitTest/Product.Api.UnitTest.csproj" />
  </Folder>
</Solution>
```

Run: `dotnet sln StoreHouse.slnx list`
Expected: os 6 projetos.

- [ ] **Step 2: Criar `README.md`**

````markdown
# StoreHouse

API REST de gestão de produtos e stock, feita para o *.NET development hands-on assessment* da Carl Zeiss Vision.

A solução tem três projetos principais:

| Projeto | Função | URL local |
|---|---|---|
| `App.IdentityServer` | Emite tokens JWT (Duende IdentityServer + ASP.NET Identity) | https://localhost:7001/swagger |
| `Product.Api` | API de produtos com os 9 endpoints pedidos (Clean Architecture + CQRS) | https://localhost:7039/swagger |
| `Product.Api.UnitTest` | Testes unitários (xUnit v3 + Moq) | — |

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (testado com 10.0.401)
- Visual Studio 2026, ou outro editor com suporte para .NET 10
- **Para correr localmente:** PostgreSQL 18 (testado com 18.6) em `localhost:5432`
- **Para correr em Docker:** Docker Desktop com Docker Compose v2
- Ferramenta do EF Core (opcional, só para gerir migrations à mão): `dotnet tool install --global dotnet-ef --version 10.0.13`

## Utilizadores de teste

> Dados de demonstração, criados pela migration do IdentityServer. No código só existe a hash das passwords.

| Utilizador | Password | Role | Permissões na Product.Api |
|---|---|---|---|
| `Admin` | `nebur#123` | Admin | Tudo |
| `Manager` | `nebur2#123` | Manager | Leituras, `PUT` e endpoints de stock |

## Correr localmente (Visual Studio ou CLI)

1. **Configurar os segredos (User Secrets).** Substituir `<PASSWORD>` pela password do utilizador `postgres` local:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:StoreHouseIdentity" "Host=localhost;Port=5432;Database=StoreHouseIdentity;Username=postgres;Password=<PASSWORD>;Pooling=true;Maximum Pool Size=50" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "IdentityServer:ClientSecret" "<um-segredo-longo-e-aleatorio>" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "ConnectionStrings:StoreHouse" "Host=localhost;Port=5432;Database=StoreHouse;Username=postgres;Password=<PASSWORD>;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50" --project Product/Product.Api
   ```

2. **Arrancar os dois projetos.** As bases de dados `StoreHouseIdentity` e `StoreHouse` são criadas, e os dados de seed inseridos, pelas migrations aplicadas automaticamente no arranque.
   - **Visual Studio:** botão direito na solução → *Configure Startup Projects…* → *Multiple startup projects*. Pôr `App.IdentityServer` e `Product.Api` em *Start* com o perfil `https`, e depois carregar em F5.
   - **CLI:** dois terminais:

     ```powershell
     dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
     dotnet run --project Product/Product.Api --launch-profile https
     ```

3. **Opcional — aplicar as migrations sem arrancar as aplicações:**

   ```powershell
   dotnet ef database update --project IdentityServer/App.IdentityServer
   dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api
   ```

## Correr em Docker

1. Criar o ficheiro de ambiente e preencher os valores:

   ```powershell
   Copy-Item .env.example .env
   notepad .env
   ```

2. Exportar e confiar no certificado HTTPS de desenvolvimento, usando a mesma password do `CERT_PASSWORD`:

   ```powershell
   New-Item -ItemType Directory -Force certs | Out-Null
   dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p "<CERT_PASSWORD>"
   dotnet dev-certs https --trust
   ```

3. Arrancar (parar antes qualquer instância local nas portas 7001 e 7039):

   ```powershell
   docker compose up --build -d
   docker compose ps
   ```

| Serviço | Endereço no host |
|---|---|
| App.IdentityServer | https://localhost:7001/swagger |
| Product.Api | https://localhost:7039/swagger |
| PostgreSQL (container) | `localhost:5433` (user e password do `.env`) |

Para parar: `docker compose down`. Para apagar também os dados: `docker compose down -v`.

## Obter um token e usá-lo no Swagger

1. Abrir https://localhost:7001/swagger → `POST /api/auth/login` → *Try it out*:

   ```json
   { "userName": "Admin", "password": "nebur#123" }
   ```

2. Copiar o valor de `accessToken` da resposta.
3. Abrir https://localhost:7039/swagger → botão **Authorize** → colar o token (sem o prefixo `Bearer`) → *Authorize*.
4. Os endpoints com cadeado passam a enviar o token. O token dura 1 hora.

## Endpoints da Product.Api

| Método | Rota | Autorização | Respostas |
|---|---|---|---|
| GET | `/api/products?lastItemId=&pageSize=5` | Pública | 200, 400 |
| GET | `/api/products/{id}` | Pública | 200, 400, 404 |
| POST | `/api/products` | **Admin** | 201, 400, 401, 403 |
| PUT | `/api/products/{id}` | Autenticado | 200, 400, 401, 404, 409 |
| DELETE | `/api/products/{id}` | **Admin** | 204, 400, 401, 403, 404 |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Autenticado | 200, 400, 401, 404, 422 |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Autenticado | 200, 400, 401, 404, 422 |
| GET | `/api/products/search?name=&lastItemId=&pageSize=5` | Pública | 200, 400 |
| GET | `/api/products/stock-level?min=&max=&lastItemId=&pageSize=5` | Pública | 200, 400 |

As listagens são paginadas por cursor (*keyset*): a resposta traz `items`, `nextCursor` e `hasMore`, e a página seguinte pede-se com `lastItemId=<nextCursor>`. Todos os produtos devolvidos incluem o stock disponível (`stockQuantity`). Os erros seguem o formato `ProblemDetails` (RFC 9457), com `code` e `traceId`.

## Testes

```powershell
dotnet test
```

Cobrem:
- os 9 endpoints do controller (sucesso e cada tipo de erro);
- as regras de autorização de cada endpoint;
- os handlers de commands e queries (com mocks);
- os limites de todas as validações;
- os behaviors do MediatR;
- o mapeamento Mapster;
- o tratamento global de exceções.

Verificação ponta a ponta, com as aplicações a correr (local ou Docker):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword 'nebur#123' -ManagerPassword 'nebur2#123'
```

## Arquitetura e decisões

```
IdentityServer/App.IdentityServer   → tokens JWT (Duende + ASP.NET Identity, BD StoreHouseIdentity)
Product/Product.Domain              → entidades (modelo anémico) e constantes
Product/Product.Application         → CQRS (MediatR), validação (FluentValidation), DTOs, padrão Result
Product/Product.Infrastructure      → EF Core + Npgsql, repositórios, UnitOfWork, migrations (BD StoreHouse)
Product/Product.Api                 → controllers, JWT, Swagger, tratamento global de exceções
UnitTests/Product.Api.UnitTest      → xUnit v3 + Moq
```

- **ID de 6 dígitos seguro com várias instâncias:** uma `SEQUENCE` do PostgreSQL (`ProductIdSequence`, 100000–999999, sem ciclo) é o valor por omissão da coluna `Id`. A base de dados garante a atomicidade, por isso várias instâncias da API nunca geram o mesmo ID.
- **Concorrência no stock:** cada alteração é um único `UPDATE` condicional (`... WHERE "Quantity" >= @q`), que corre na mesma transação do registo em `StockMovement`. Pedidos simultâneos nunca deixam o stock negativo, e um `CHECK (Quantity >= 0)` serve de última defesa.
- **Concorrência no `PUT` e no `DELETE`:** usam concorrência otimista com a coluna de sistema `xmin` do PostgreSQL. Um conflito devolve `409`.
- **Soft delete:** o `DELETE` marca `IsDeleted`, `DeletedAt` e `DeletedBy`. Um filtro global do EF Core esconde os produtos apagados, e o histórico de stock é mantido.
- **Erros:** o padrão `Result` é usado para resultados esperados (validação, não encontrado, conflito, regra de negócio). As exceções inesperadas são tratadas por um `IExceptionHandler` global, que devolve `500` em `ProblemDetails`. A Application não conhece HTTP.
- **Performance:**
  - `AsNoTracking` e projeção direta para DTO nas leituras;
  - paginação *keyset*;
  - índice trigram (`pg_trgm`) para a pesquisa parcial por nome;
  - índice em `Stock.Quantity` para a pesquisa por intervalo;
  - `DbContext` pooling e connection pooling do Npgsql.
- **Segurança:**
  - queries sempre parametrizadas, e os caracteres especiais do `ILIKE` são escapados;
  - segredos fora do código (User Secrets e `.env`);
  - containers sem privilégios de administrador;
  - lockout após 5 tentativas de login falhadas.

## Licenças

- **Duende IdentityServer:** gratuito em desenvolvimento e testes, mas precisa de licença em produção (ver [duendesoftware.com](https://duendesoftware.com/products/identityserver)). Sem licença, o log mostra avisos, o que é esperado neste exercício. O grant *password* foi usado por ser o fluxo pedido (username e password → token); em produção, o recomendado é *authorization code + PKCE*.
- **MediatR 12.5.0:** a última versão com licença Apache 2.0. As versões 13 e seguintes têm licença comercial.
- **Mapster, FluentValidation, Swashbuckle, Npgsql, xUnit e Moq:** licenças open source.
````

- [ ] **Step 3: Rever o README contra a solução real**

Confirmar que:
- os comandos do README correspondem aos dos Planos 01 e 02;
- os caminhos `IdentityServer/App.IdentityServer`, `Product/Product.Api` e `Product/Product.Infrastructure` existem;
- as portas 7001, 7039 e 5433 estão certas.

Run:
```powershell
Test-Path IdentityServer/App.IdentityServer, Product/Product.Api, Product/Product.Infrastructure, scripts/smoke-test.ps1, docker-compose.yml, .env.example
```
Expected: `True` 6 vezes.

- [ ] **Step 4: Checkpoint final da solução**

Run:
```powershell
dotnet build StoreHouse.slnx
dotnet test StoreHouse.slnx
```
Expected: `Build succeeded`, e todos os testes passam.

Rever a checklist do enunciado:
- [ ] 9 endpoints implementados (Plano 02, Tasks 9 e 10; smoke test)
- [ ] ID de 6 dígitos seguro com várias instâncias (sequence; Plano 02, Task 8)
- [ ] Migrations EF code-first com seed (Planos 01 e 02)
- [ ] Testes unitários de vários cenários (`Product.Api.UnitTest`)
- [ ] Documentação para correr localmente (`README.md`)
- [ ] Código limpo, executável e testável (`dotnet build`, `dotnet test`, smoke test local e em Docker)

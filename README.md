# StoreHouse

API REST de gestão de produtos e stock, feita para um *.NET development hands-on assessment*.

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

1. **Confiar no certificado HTTPS de desenvolvimento do .NET.** O endpoint de login do IdentityServer chama o seu próprio endpoint de tokens por HTTPS, por isso o certificado tem de ser de confiança:

   ```powershell
   dotnet dev-certs https --trust
   ```

2. **Configurar os segredos (User Secrets).** Substituir `<PASSWORD>` pela password do utilizador `postgres` local:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:StoreHouseIdentity" "Host=localhost;Port=5432;Database=StoreHouseIdentity;Username=postgres;Password=<PASSWORD>;Pooling=true;Maximum Pool Size=50" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "IdentityServer:ClientSecret" "<um-segredo-longo-e-aleatorio>" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "ConnectionStrings:StoreHouse" "Host=localhost;Port=5432;Database=StoreHouse;Username=postgres;Password=<PASSWORD>;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50" --project Product/Product.Api
   ```

3. **Arrancar os dois projetos.** As bases de dados `StoreHouseIdentity` e `StoreHouse` são criadas, e os dados de seed inseridos, pelas migrations aplicadas automaticamente no arranque.
   - **Visual Studio:** botão direito na solução → *Configure Startup Projects…* → *Multiple startup projects*. Pôr `App.IdentityServer` e `Product.Api` em *Start* com o perfil `https`, e depois carregar em F5.
   - **CLI:** dois terminais:

     ```powershell
     dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
     dotnet run --project Product/Product.Api --launch-profile https
     ```

4. **Opcional — aplicar as migrations sem arrancar as aplicações:**

   ```powershell
   dotnet ef database update --project IdentityServer/App.IdentityServer
   dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api
   ```

## Correr em Docker

O `docker-compose.yml` está na raiz do projeto. O `.env` cria-se na mesma pasta: o Compose lê-o aí para substituir `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}`, `${CERT_PASSWORD}` e `${IDENTITY_CLIENT_SECRET}`. Corre os comandos desta secção nessa pasta; noutra pasta o Compose responde `no configuration file provided: not found`.

1. Na raiz do projeto, criar o `.env` a partir do exemplo e preencher os valores:

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

3. Arrancar a partir da raiz do projeto (parar antes qualquer instância local nas portas 7001 e 7039):

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
Todas as respostas de erro (incluindo `401`, `403`, rotas inexistentes e `500`) usam o `Content-Type` `application/problem+json`.

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
- **Concorrência no `PUT` e no `DELETE`:** usam concorrência otimista com a coluna de sistema `xmin` do PostgreSQL. Se outro pedido gravar o mesmo produto entre a leitura e a gravação do pedido em curso, devolve `409`. A proteção não cobre o cliente que envia um `PUT` com dados lidos antes e entretanto desatualizados: o `xmin` não é exposto na API, por isso essa alteração é gravada e sobrescreve a anterior (ver [Architecture.md](docs/Architecture.md)).
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

## Limitações conhecidas

- **Docker em `Development`:** o `docker compose` corre as aplicações com `ASPNETCORE_ENVIRONMENT=Development`, por isso o Swagger está ativo e as respostas `500` incluem os detalhes da exceção. Serve apenas para demonstração.
- **Janela de retry no stock:** se a ligação cair depois de um commit bem-sucedido, a execution strategy pode repetir a alteração de stock (muito raro). Uma versão de produção usaria uma chave de idempotência.
- **Licença do Duende IdentityServer:** é necessária em produção (ver [Licenças](#licenças)).

## Licenças

- **Duende IdentityServer:** gratuito em desenvolvimento e testes, mas precisa de licença em produção (ver [duendesoftware.com](https://duendesoftware.com/products/identityserver)). Sem licença, o log mostra avisos, o que é esperado neste exercício. O grant *password* foi usado por ser o fluxo pedido (username e password → token); em produção, o recomendado é *authorization code + PKCE*.
- **MediatR 12.5.0:** a última versão com licença Apache 2.0. As versões 13 e seguintes têm licença comercial.
- **Mapster, FluentValidation, Swashbuckle, Npgsql, xUnit e Moq:** licenças open source.

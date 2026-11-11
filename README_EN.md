# StoreHouse

REST API for product and stock management, built for a *.NET development hands-on assessment*.

The solution has three main projects:

| Project | Role | Local URL |
|---|---|---|
| `App.IdentityServer` | Issues JWT tokens (Duende IdentityServer + ASP.NET Identity) | https://localhost:7001/swagger |
| `Product.Api` | Product API with the 9 required endpoints (Clean Architecture + CQRS) | https://localhost:7039/swagger |
| `Product.Api.UnitTest` | Unit tests (xUnit v3 + Moq) | — |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (tested with 10.0.401)
- Visual Studio 2026, or another editor with .NET 10 support
- **To run locally:** PostgreSQL 18 (tested with 18.6) on `localhost:5432`
- **To run in Docker:** Docker Desktop with Docker Compose v2
- EF Core tool (optional, only to manage migrations by hand): `dotnet tool install --global dotnet-ef --version 10.0.13`

## Test users

> Demo data, created by the IdentityServer migration. Only the password hashes exist in the code.

| User | Password | Role | Permissions on Product.Api |
|---|---|---|---|
| `Admin` | `nebur#123` | Admin | Everything |
| `Manager` | `nebur2#123` | Manager | Reads, `PUT` and stock endpoints |

## Run locally (Visual Studio or CLI)

1. **Trust the .NET HTTPS development certificate.** The IdentityServer login endpoint calls its own token endpoint over HTTPS, so the certificate must be trusted:

   ```powershell
   dotnet dev-certs https --trust
   ```

2. **Configure the secrets (User Secrets).** Replace `<PASSWORD>` with the password of the local `postgres` user:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:StoreHouseIdentity" "Host=localhost;Port=5432;Database=StoreHouseIdentity;Username=postgres;Password=<PASSWORD>;Pooling=true;Maximum Pool Size=50" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "IdentityServer:ClientSecret" "<a-long-random-secret>" --project IdentityServer/App.IdentityServer
   dotnet user-secrets set "ConnectionStrings:StoreHouse" "Host=localhost;Port=5432;Database=StoreHouse;Username=postgres;Password=<PASSWORD>;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50" --project Product/Product.Api
   ```

3. **Start both projects.** The `StoreHouseIdentity` and `StoreHouse` databases are created, and the seed data inserted, by the migrations applied automatically on startup.
   - **Visual Studio:** right-click the solution → *Configure Startup Projects…* → *Multiple startup projects*. Set `App.IdentityServer` and `Product.Api` to *Start* with the `https` profile, then press F5.
   - **CLI:** two terminals:

     ```powershell
     dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
     dotnet run --project Product/Product.Api --launch-profile https
     ```

4. **Optional — apply the migrations without starting the applications:**

   ```powershell
   dotnet ef database update --project IdentityServer/App.IdentityServer
   dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api
   ```

## Run in Docker

`docker-compose.yml` is in the project root. The `.env` file is created in the same folder: Compose reads it there to substitute `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}`, `${CERT_PASSWORD}` and `${IDENTITY_CLIENT_SECRET}`. Run the commands in this section from that folder; from any other folder Compose answers `no configuration file provided: not found`.

1. In the project root, create the `.env` from the example and fill in the values:

   ```powershell
   Copy-Item .env.example .env
   notepad .env
   ```

2. Export and trust the HTTPS development certificate, using the same password as `CERT_PASSWORD`:

   ```powershell
   New-Item -ItemType Directory -Force certs | Out-Null
   dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p "<CERT_PASSWORD>"
   dotnet dev-certs https --trust
   ```

3. Start from the project root (first stop any local instance on ports 7001 and 7039):

   ```powershell
   docker compose up --build -d
   docker compose ps
   ```

| Service | Address on the host |
|---|---|
| App.IdentityServer | https://localhost:7001/swagger |
| Product.Api | https://localhost:7039/swagger |
| PostgreSQL (container) | `localhost:5433` (user and password from `.env`) |

To stop: `docker compose down`. To also delete the data: `docker compose down -v`.

## Get a token and use it in Swagger

1. Open https://localhost:7001/swagger → `POST /api/auth/login` → *Try it out*:

   ```json
   { "userName": "Admin", "password": "nebur#123" }
   ```

2. Copy the `accessToken` value from the response.
3. Open https://localhost:7039/swagger → **Authorize** button → paste the token (without the `Bearer` prefix) → *Authorize*.
4. Endpoints with a padlock now send the token. The token lasts 1 hour.

## Product.Api endpoints

| Method | Route | Authorization | Responses |
|---|---|---|---|
| GET | `/api/products?lastItemId=&pageSize=5` | Public | 200, 400 |
| GET | `/api/products/{id}` | Public | 200, 400, 404 |
| POST | `/api/products` | **Admin** | 201, 400, 401, 403 |
| PUT | `/api/products/{id}` | Authenticated | 200, 400, 401, 404, 409 |
| DELETE | `/api/products/{id}` | **Admin** | 204, 400, 401, 403, 404 |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Authenticated | 200, 400, 401, 404, 422 |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Authenticated | 200, 400, 401, 404, 422 |
| GET | `/api/products/search?name=&lastItemId=&pageSize=5` | Public | 200, 400 |
| GET | `/api/products/stock-level?min=&max=&lastItemId=&pageSize=5` | Public | 200, 400 |

Listings are cursor (*keyset*) paginated: the response contains `items`, `nextCursor` and `hasMore`, and the next page is requested with `lastItemId=<nextCursor>`. All returned products include the available stock (`stockQuantity`). Errors follow the `ProblemDetails` format (RFC 9457), with `code` and `traceId`.
All error responses (including `401`, `403`, unknown routes and `500`) use the `application/problem+json` `Content-Type`.

## Tests

```powershell
dotnet test
```

They cover:
- the 9 controller endpoints (success and each error type);
- the authorization rules of each endpoint;
- the command and query handlers (with mocks);
- the limits of all validations;
- the MediatR behaviors;
- the Mapster mapping;
- global exception handling.

End-to-end verification, with the applications running (local or Docker):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword 'nebur#123' -ManagerPassword 'nebur2#123'
```

## Architecture and decisions

```
IdentityServer/App.IdentityServer   → JWT tokens (Duende + ASP.NET Identity, StoreHouseIdentity database)
Product/Product.Domain              → entities (anemic model) and constants
Product/Product.Application         → CQRS (MediatR), validation (FluentValidation), DTOs, Result pattern
Product/Product.Infrastructure      → EF Core + Npgsql, repositories, UnitOfWork, migrations (StoreHouse database)
Product/Product.Api                 → controllers, JWT, Swagger, global exception handling
UnitTests/Product.Api.UnitTest      → xUnit v3 + Moq
```

- **6-digit ID safe with multiple instances:** a PostgreSQL `SEQUENCE` (`ProductIdSequence`, 100000–999999, no cycle) is the default value of the `Id` column. The database guarantees atomicity, so several API instances never generate the same ID.
- **Stock concurrency:** each change is a single conditional `UPDATE` (`... WHERE "Quantity" >= @q`), which runs in the same transaction as the `StockMovement` record. Simultaneous requests never leave the stock negative, and a `CHECK (Quantity >= 0)` serves as the last line of defense.
- **Concurrency on `PUT` and `DELETE`:** they use optimistic concurrency with the PostgreSQL `xmin` system column. If another request saves the same product between the current request's read and write, it returns `409`. The protection does not cover a client sending a `PUT` with data read earlier that has since gone stale: `xmin` is not exposed by the API, so that change is saved and overwrites the previous one (see [Architecture_EN.md](docs/Architecture_EN.md)).
- **Soft delete:** `DELETE` sets `IsDeleted`, `DeletedAt` and `DeletedBy`. A global EF Core filter hides deleted products, and the stock history is kept.
- **Errors:** the `Result` pattern is used for expected outcomes (validation, not found, conflict, business rule). Unexpected exceptions are handled by a global `IExceptionHandler`, which returns `500` as `ProblemDetails`. The Application layer knows nothing about HTTP.
- **Performance:**
  - `AsNoTracking` and direct projection to DTOs on reads;
  - *keyset* pagination;
  - trigram index (`pg_trgm`) for partial name search;
  - index on `Stock.Quantity` for range search;
  - `DbContext` pooling and Npgsql connection pooling.
- **Security:**
  - queries are always parameterized, and the special characters of `ILIKE` are escaped;
  - secrets kept out of the code (User Secrets and `.env`);
  - containers without administrator privileges;
  - lockout after 5 failed login attempts.

## Known limitations

- **Docker in `Development`:** `docker compose` runs the applications with `ASPNETCORE_ENVIRONMENT=Development`, so Swagger is enabled and `500` responses include the exception details. For demonstration only.
- **Stock retry window:** if the connection drops after a successful commit, the execution strategy may repeat the stock change (very rare). A production version would use an idempotency key.
- **Duende IdentityServer license:** required in production (see [Licenses](#licenses)).

## Licenses

- **Duende IdentityServer:** free for development and testing, but requires a license in production (see [duendesoftware.com](https://duendesoftware.com/products/identityserver)). Without a license, the log shows warnings, which is expected in this exercise. The *password* grant was used because it is the requested flow (username and password → token); in production, *authorization code + PKCE* is recommended.
- **MediatR 12.5.0:** the last version under the Apache 2.0 license. Versions 13 and later have a commercial license.
- **Mapster, FluentValidation, Swashbuckle, Npgsql, xUnit and Moq:** open source licenses.

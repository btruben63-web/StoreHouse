# Runbook

Short operational guide. Steps for using the API (getting a token, endpoints, test users) are in the [README](../README_EN.md); architecture is in [Architecture_EN.md](Architecture_EN.md). All commands run from the repository root, in PowerShell.

## 1. Prerequisites

| Needed for | Requirement |
|---|---|
| Everything | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (verified with 10.0.401) |
| Local run | PostgreSQL on `localhost:5432` (the README mentions 18.x) |
| Docker run | Docker with Compose v2 (verified with Docker 29.9.2) |
| Manual migrations (optional) | `dotnet tool install --global dotnet-ef --version 10.0.13` |
| Local HTTPS | `dotnet dev-certs https --trust` |

## 2. Configuration

Values are read from `appsettings*.json`, User Secrets or environment variables (`:` becomes `__`, for example `ConnectionStrings__StoreHouse`).

**App.IdentityServer**

| Key | Required | Default (`appsettings.json`) |
|---|---|---|
| `ConnectionStrings:StoreHouseIdentity` | Yes | — (User Secrets / env) |
| `IdentityServer:ClientSecret` | Yes | — (User Secrets / env) |
| `IdentityServer:IssuerUri` | Yes | `https://localhost:7001` |
| `IdentityServer:TokenEndpoint` | Yes | `https://localhost:7001/connect/token` |
| `IdentityServer:ClientId` | Yes | `storehouse.swagger` |
| `IdentityServer:ApiScope` | Yes | `product.api` |
| `IdentityServer:AccessTokenLifetimeSeconds` | No | `3600` |
| `IdentityServer:KeyPath` | No | not set (Duende uses its own default path; in Compose it is `/home/app/keys`) |

**Product.Api**

| Key | Required | Default (`appsettings.json`) |
|---|---|---|
| `ConnectionStrings:StoreHouse` | Yes | — (User Secrets / env) |
| `Jwt:Authority` | Yes | `https://localhost:7001` |
| `Jwt:Issuer` | Yes | `https://localhost:7001` (must match `IdentityServer:IssuerUri`) |
| `Jwt:Audience` | Yes | `product.api` (must match `IdentityServer:ApiScope`) |
| `Jwt:RequireHttpsMetadata` | No | `true` |
| `Jwt:MetadataAddress` | No | not set |

**Docker Compose (`.env`, created from `.env.example`; the file is in `.gitignore`)**

`POSTGRES_USER`, `POSTGRES_PASSWORD`, `CERT_PASSWORD`, `IDENTITY_CLIENT_SECRET`. Compose injects them into the connection strings, the Kestrel certificate and `IdentityServer__ClientSecret`.

## 3. Install and run

### Local

1. Trust the development certificate: `dotnet dev-certs https --trust`.
2. Set the secrets (full commands in the [README](../README_EN.md#run-locally-visual-studio-or-cli)): `ConnectionStrings:StoreHouseIdentity`, `IdentityServer:ClientSecret` (project `IdentityServer/App.IdentityServer`) and `ConnectionStrings:StoreHouse` (project `Product/Product.Api`), using `dotnet user-secrets set`.
3. Start both projects in two terminals:

```powershell
dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
dotnet run --project Product/Product.Api --launch-profile https
```

The `StoreHouseIdentity` and `StoreHouse` databases are created and populated by the migrations on startup.

### Docker

```powershell
Copy-Item .env.example .env          # fill in the values
New-Item -ItemType Directory -Force certs | Out-Null
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p "<CERT_PASSWORD>"
dotnet dev-certs https --trust
docker compose up --build -d
docker compose ps
```

### Addresses

| Service | Local | Docker (host) |
|---|---|---|
| App.IdentityServer | `https://localhost:7001/swagger` (HTTP: `http://localhost:5001`) | `https://localhost:7001/swagger` |
| Product.Api | `https://localhost:7039/swagger` (HTTP: `http://localhost:5039`) | `https://localhost:7039/swagger` |
| PostgreSQL | `localhost:5432` | `localhost:5433` |

Swagger is only enabled with `ASPNETCORE_ENVIRONMENT=Development` (set in the `https` profiles and in Compose).

## 4. Tests and validation

| Goal | Command |
|---|---|
| Build the solution | `dotnet build` |
| Unit tests | `dotnet test` (139 tests passed at the last verification) |
| End-to-end verification (apps running) | `powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword '<Admin-password>' -ManagerPassword '<Manager-password>'` |
| Validate Compose | `docker compose config -q` |

Demo user passwords: see the [README](../README_EN.md#test-users). The smoke test also accepts `-IdentityUrl` (default `https://localhost:7001`) and `-ApiUrl` (default `https://localhost:7039`) and relies on the seed data, so it fails if the seed products have been changed.

## 5. Useful commands

```powershell
# Migrations without starting the applications
dotnet ef database update --project IdentityServer/App.IdentityServer
dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api

# Docker
docker compose logs -f product-api         # services: postgres, app-identityserver, product-api
docker compose restart product-api
docker compose down                        # stop
docker compose down -v                     # stop and delete data (databases and signing keys)
```

The seed password hashes are precomputed in `IdentitySeedData.cs`; if a seed password changes, they must be regenerated (note in the file itself).

## 6. Known issues

- In Docker the apps run with `ASPNETCORE_ENVIRONMENT=Development`: Swagger is enabled and `500` responses include the exception detail. Demo use only.
- If the database connection drops after a successful stock commit, the *execution strategy* may repeat the operation (no idempotency key).
- Duende IdentityServer requires a license in production; without one the log shows warnings.
- There are no integration tests and no coverage of persistence/migrations: failures in that layer are only detected by running the applications.

## 7. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| Startup fails with `Connection string '...' is not configured.` | Missing secret/variable | Set `ConnectionStrings:StoreHouse` (API) or `ConnectionStrings:StoreHouseIdentity` (IdentityServer) |
| IdentityServer fails with `Configuration value 'IdentityServer:...' is required.` | Required key is empty (typically `ClientSecret`) | Set the secret (User Secrets or `IDENTITY_CLIENT_SECRET`) |
| `POST /api/auth/login` returns `500` | Login calls the `TokenEndpoint` over HTTPS; any error other than invalid credentials throws an exception (for example an untrusted certificate or an unreachable endpoint) | `dotnet dev-certs https --trust`; check `IdentityServer:TokenEndpoint` |
| `POST /api/auth/login` returns `401` | Invalid credentials (`invalid_grant` from the token endpoint). Lockout (5 failures, 5 minutes) is configured in Identity; the code does not handle it separately | Check the user name/password; wait for the lockout to end |
| Product.Api returns `401` with a valid token | `Jwt:Issuer`/`Jwt:Audience` differ from `IdentityServer:IssuerUri`/`ApiScope`; token expired (1 h); `Authority`/`MetadataAddress` unreachable from the API | Align the values; request a new token; in Docker the API uses `http://app-identityserver:8080` |
| `403` on `POST`/`DELETE` | `Admin` role only | Use an `Admin` token |
| `400` on `/{id}` or `lastItemId` | Valid IDs: 100000–999999; `pageSize` 1–50; `name` 2–100 characters; `min` ≥ 0 and `max` ≥ `min` | Fix the parameters |
| `409` on `PUT`/`DELETE` | Another request saved the same product between the current request's read and write (`xmin`). It is not returned when the client sends a `PUT` with data read earlier that has since gone stale: in that case the change is saved and overwrites the previous one | Reload the product and retry |
| `422` on stock | Insufficient stock (`decrement-stock`) or stock limit exceeded (`add-to-stock`); quantity per operation: 1–1,000,000 | Adjust the quantity |
| `docker compose` answers `no configuration file provided: not found` | Command run outside the root | Run it in the folder containing `docker-compose.yml` |
| Containers fail to start because ports are in use | Ports 7001, 7039 or 5433 are taken | Stop the local instance or the process using them |
| Certificate error in the containers | `certs/aspnetapp.pfx` missing or `CERT_PASSWORD` differs from the one used when exporting | Export the certificate again with the same password |
| Changing `POSTGRES_USER`/`POSTGRES_PASSWORD` has no effect | The `pgdata` volume keeps the credentials from the first initialization (behavior of the `postgres` image) | `docker compose down -v` (deletes data) |
| `/swagger` returns `404` | Environment other than `Development` | Set `ASPNETCORE_ENVIRONMENT=Development` |

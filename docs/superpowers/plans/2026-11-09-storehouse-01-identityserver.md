# StoreHouse — Plano 01: Solução + App.IdentityServer

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar a solução `StoreHouse` com a configuração partilhada e o projeto `App.IdentityServer`. Este projeto emite tokens JWT (Duende IdentityServer + ASP.NET Identity em PostgreSQL) para os utilizadores `Admin` e `Manager`, através de `POST /api/auth/login`.

**Architecture:** Um Web API ASP.NET Core (.NET 10) com controllers e Swagger.
- O ASP.NET Identity guarda os utilizadores, as roles e as claims das roles na base `StoreHouseIdentity`, criada por migrations com seed `HasData`.
- O Duende IdentityServer emite os access tokens com o grant `password`.
- O endpoint de login chama internamente `/connect/token`.

**Tech Stack:** .NET 10, C# 14, Duende IdentityServer 8.0.10, Duende.IdentityModel 8.1.0, ASP.NET Core Identity + EF Core 10.0.13, Npgsql EF 10.0.3, Swashbuckle 10.3.1, PostgreSQL 18.

**Spec:** `docs/superpowers/specs/2026-11-08-storehouse-design.md`. A secção 10 do spec prevalece sobre as anteriores.

**Ordem dos planos:**
1. Este plano.
2. `2026-11-09-storehouse-02-product-api.md`.
3. `2026-11-09-storehouse-03-docker-readme.md`.

## Global Constraints

- Raiz da solução: `C:\Users\Ruben Batista\OneDrive\Documents\Projetos\StoreHouse` (abreviada como `StoreHouse/`). Todos os comandos correm a partir desta pasta, salvo indicação em contrário.
- Plataforma: .NET 10 (`net10.0`) e C# 14. SDK instalado: 10.0.401. Shell: Windows PowerShell.
- **Sem git:** não criar repositório nem fazer commits. Cada task termina com um checkpoint de build e verificação.
- Nomes de métodos, variáveis, configuração, tabelas, colunas e índices em inglês. Classes DTO terminam sempre em `DTO`.
- PascalCase para tipos, métodos e propriedades; camelCase para variáveis locais e parâmetros.
- Utilizadores: `Admin` / `nebur#123` com role `Admin`, e `Manager` / `nebur2#123` com role `Manager`. No código só fica a hash da password, nunca o texto.
- Segredos (connection strings, client secret) nunca ficam em `appsettings*.json`: vão para User Secrets (local) ou variáveis de ambiente (Docker).
- Swagger: Swashbuckle.AspNetCore 10.3.1, que usa o Microsoft.OpenApi 2.x (namespace `Microsoft.OpenApi`). Não usar o `Microsoft.AspNetCore.OpenApi`.
- Portas locais do IdentityServer: `https://localhost:7001` e `http://localhost:5001`. `IssuerUri` fixo: `https://localhost:7001`.
- Todos os pacotes têm a versão definida em `Directory.Packages.props`. Os `.csproj` não têm atributo `Version`.

## Mapa de ficheiros

```
StoreHouse/
├── StoreHouse.slnx                                   (Task 1)
├── Directory.Build.props                             (Task 1)
├── Directory.Packages.props                          (Task 1, pacotes de toda a solução)
├── .gitignore                                        (Task 1)
├── .dockerignore                                     (Task 1)
└── IdentityServer/App.IdentityServer/
    ├── App.IdentityServer.csproj                     (Task 1)
    ├── Program.cs                                    (Task 1 → 2 → 3)
    ├── appsettings.json                              (Task 1 → 3)
    ├── appsettings.Development.json                  (Task 1)
    ├── Properties/launchSettings.json                (Task 1)
    ├── Dockerfile                                    (Task 1)
    ├── Authorization/AppRoles.cs                     (Task 2)
    ├── Data/AppIdentityDbContext.cs                  (Task 2)
    ├── Data/Seed/IdentitySeedData.cs                 (Task 2)
    ├── Data/Seed/RoleSeedConfiguration.cs            (Task 2)
    ├── Data/Seed/UserSeedConfiguration.cs            (Task 2)
    ├── Data/Seed/UserRoleSeedConfiguration.cs        (Task 2)
    ├── Data/Seed/RoleClaimSeedConfiguration.cs       (Task 2)
    ├── Data/Migrations/*                             (Task 2, gerado pelo dotnet ef)
    ├── Configuration/IdentityServerSettings.cs       (Task 3)
    ├── Configuration/IdentityServerConfig.cs         (Task 3)
    ├── Services/DistinctClaimsProfileService.cs      (Task 3)
    ├── DTOs/LoginRequestDTO.cs                       (Task 3)
    ├── DTOs/TokenResponseDTO.cs                      (Task 3)
    └── Controllers/AuthController.cs                 (Task 3)
```

Este projeto não tem testes unitários (o spec só pede testes para a Product.Api). A verificação de cada task é feita com comandos e com as respostas HTTP esperadas.

---

### Task 1: Solução, configuração partilhada e esqueleto do App.IdentityServer

Entregável: `StoreHouse.slnx` compila, e o App.IdentityServer arranca com o Swagger em `https://localhost:7001/swagger`.

**Files:**
- Create: `StoreHouse.slnx` (gerado), `Directory.Build.props`, `Directory.Packages.props`, `.gitignore`, `.dockerignore`
- Create: `IdentityServer/App.IdentityServer/App.IdentityServer.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`, `Dockerfile`

**Interfaces:**
- Produces: `Directory.Packages.props` com as versões de todos os pacotes usados nos planos 01, 02 e 03; o perfil de launch `https` na porta 7001; a pasta de solução `IdentityServer`.

- [ ] **Step 1: Criar a solução**

Run:
```powershell
cd "C:\Users\Ruben Batista\OneDrive\Documents\Projetos\StoreHouse"
dotnet new sln -n StoreHouse
Get-ChildItem StoreHouse.slnx
```
Expected: o ficheiro `StoreHouse.slnx` existe (no SDK 10 o formato por omissão é o `.slnx`).

- [ ] **Step 2: Criar `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Criar `Directory.Packages.props`**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup Label="ASP.NET Core e EF Core">
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.13" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.13" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.13" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.13" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.13" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.3.1" />
    <PackageVersion Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.23.0" />
  </ItemGroup>
  <ItemGroup Label="Identidade">
    <PackageVersion Include="Duende.IdentityServer" Version="8.0.10" />
    <PackageVersion Include="Duende.IdentityServer.AspNetIdentity" Version="8.0.10" />
    <PackageVersion Include="Duende.IdentityModel" Version="8.1.0" />
  </ItemGroup>
  <ItemGroup Label="Application">
    <PackageVersion Include="MediatR" Version="12.5.0" />
    <PackageVersion Include="Mapster" Version="10.0.14" />
    <PackageVersion Include="FluentValidation" Version="12.2.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0" />
  </ItemGroup>
  <ItemGroup Label="Testes">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.11.0" />
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.11.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Criar `.gitignore` e acrescentar as exclusões do projeto**

Run:
```powershell
dotnet new gitignore
Add-Content .gitignore "`n# StoreHouse`n.env`ncerts/`n**/keys/"
```
Expected: `.gitignore` criado, com as 3 linhas finais `.env`, `certs/` e `**/keys/`.

- [ ] **Step 5: Criar `.dockerignore`**

```
**/.git
**/.vs
**/.vscode
**/bin
**/obj
**/*.user
**/.env
**/certs
**/keys
**/docs
**/UnitTests
**/scripts
**/Dockerfile*
**/docker-compose*
**/README.md
```

- [ ] **Step 6: Criar `IdentityServer/App.IdentityServer/App.IdentityServer.csproj`**

Equivale, no Visual Studio, a: *ASP.NET Core Web API*, .NET 10, Authentication None, HTTPS, container Linux com Dockerfile, OpenAPI, top-level statements e controllers.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>b7d3c1e2-6a4f-4f0e-9d2b-3c5a7e8f1a20</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..</DockerfileContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Duende.IdentityModel" />
    <PackageReference Include="Duende.IdentityServer" />
    <PackageReference Include="Duende.IdentityServer.AspNetIdentity" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Criar `Program.cs` (versão inicial, só com Swagger)**

```csharp
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Identity", Version = "v1" }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 8: Criar `appsettings.json` e `appsettings.Development.json`**

`appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Duende": "Information"
    }
  }
}
```

- [ ] **Step 9: Criar `Properties/launchSettings.json`**

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Container (Dockerfile)": {
      "commandName": "Docker",
      "launchBrowser": true,
      "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}/swagger",
      "environmentVariables": {
        "ASPNETCORE_HTTPS_PORTS": "8081",
        "ASPNETCORE_HTTP_PORTS": "8080"
      },
      "publishAllPorts": true,
      "useSSL": true
    }
  }
}
```

- [ ] **Step 10: Criar `Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["IdentityServer/App.IdentityServer/App.IdentityServer.csproj", "IdentityServer/App.IdentityServer/"]
RUN dotnet restore "IdentityServer/App.IdentityServer/App.IdentityServer.csproj"
COPY ["IdentityServer/App.IdentityServer/", "IdentityServer/App.IdentityServer/"]
WORKDIR /src/IdentityServer/App.IdentityServer
RUN dotnet publish "App.IdentityServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
RUN mkdir -p /home/app/keys
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "App.IdentityServer.dll"]
```

A pasta `/home/app/keys` é criada pelo utilizador `app`. É lá que, no Docker, o Duende guarda as chaves de assinatura (Plano 03).

- [ ] **Step 11: Adicionar o projeto à solução, na pasta `IdentityServer`**

Run:
```powershell
dotnet sln StoreHouse.slnx add IdentityServer/App.IdentityServer/App.IdentityServer.csproj --solution-folder IdentityServer
```
Expected: `Project ... added to the solution.`

- [ ] **Step 12: Compilar**

Run: `dotnet build StoreHouse.slnx`
Expected: `Build succeeded` com 0 erros.

- [ ] **Step 13: Checkpoint — arrancar e ver o Swagger**

Run: `dotnet run --project IdentityServer/App.IdentityServer --launch-profile https`
Expected:
- o log mostra `Now listening on: https://localhost:7001`;
- `https://localhost:7001/swagger` abre a página "StoreHouse Identity", com a mensagem "No operations defined in spec!".

Parar com `Ctrl+C`.

---

### Task 2: Base de dados StoreHouseIdentity, com seed de roles, utilizadores e claims

Entregável: a migration `InitialIdentity` cria as tabelas do ASP.NET Identity na base `StoreHouseIdentity` e insere as 2 roles, os 2 utilizadores, as associações utilizador–role e as 2 role claims.

**Files:**
- Create: `IdentityServer/App.IdentityServer/Authorization/AppRoles.cs`
- Create: `IdentityServer/App.IdentityServer/Data/AppIdentityDbContext.cs`
- Create: `IdentityServer/App.IdentityServer/Data/Seed/IdentitySeedData.cs`
- Create: `IdentityServer/App.IdentityServer/Data/Seed/RoleSeedConfiguration.cs`
- Create: `IdentityServer/App.IdentityServer/Data/Seed/UserSeedConfiguration.cs`
- Create: `IdentityServer/App.IdentityServer/Data/Seed/UserRoleSeedConfiguration.cs`
- Create: `IdentityServer/App.IdentityServer/Data/Seed/RoleClaimSeedConfiguration.cs`
- Modify: `IdentityServer/App.IdentityServer/Program.cs` (substituir na totalidade)
- Generated: `IdentityServer/App.IdentityServer/Data/Migrations/*`

**Interfaces:**
- Consumes: o projeto da Task 1.
- Produces:
  - `AppIdentityDbContext : IdentityDbContext<IdentityUser>`;
  - `AppRoles.Admin = "Admin"` e `AppRoles.Manager = "Manager"`;
  - a connection string `ConnectionStrings:StoreHouseIdentity`;
  - as tabelas `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` e `AspNetRoleClaims` com dados.

- [ ] **Step 1: Configurar a connection string em User Secrets**

Usa a password do teu utilizador `postgres` local. A base de dados ainda não existe: é a migration que a cria.

Run:
```powershell
dotnet user-secrets set "ConnectionStrings:StoreHouseIdentity" "Host=localhost;Port=5432;Database=StoreHouseIdentity;Username=postgres;Password=<A_TUA_PASSWORD>;Pooling=true;Maximum Pool Size=50" --project IdentityServer/App.IdentityServer
dotnet user-secrets list --project IdentityServer/App.IdentityServer
```
Expected: a lista mostra `ConnectionStrings:StoreHouseIdentity = Host=localhost;...`.

- [ ] **Step 2: Criar `Authorization/AppRoles.cs`**

```csharp
namespace App.IdentityServer.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
}
```

- [ ] **Step 3: Criar `Data/AppIdentityDbContext.cs`**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.IdentityServer.Data;

public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppIdentityDbContext).Assembly);
    }
}
```

- [ ] **Step 4: Criar `Data/Seed/IdentitySeedData.cs`**

As hashes abaixo foram geradas com `PasswordHasher<IdentityUser>` (ASP.NET Identity V3) e verificadas com `VerifyHashedPassword`, que devolveu `Success`. Os stamps são fixos para que novas migrations não detetem alterações falsas.

```csharp
namespace App.IdentityServer.Data.Seed;

internal static class IdentitySeedData
{
    public const string AdminRoleId = "6b0e1c2a-3f4d-4a5b-9c6d-7e8f9a0b1c2d";
    public const string ManagerRoleId = "7c1f2d3b-4a5e-4b6c-8d7e-8f9a0b1c2d3e";
    public const string AdminRoleConcurrencyStamp = "0f6a8f43-2c55-4d1e-9a3b-6e2d4c8b1a70";
    public const string ManagerRoleConcurrencyStamp = "8d2b5c71-9e4f-4a3d-b6c2-1f7e9a0d3b54";

    public const string AdminUserId = "1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d";
    public const string ManagerUserId = "2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e";
    public const string AdminSecurityStamp = "W5J7QK3XN4LZ8RT6Y2PV9CHUDMBFGSAE";
    public const string ManagerSecurityStamp = "H3M8TZ6QW2KD9XRV5NLC7YBPJ4FSGAUE";
    public const string AdminConcurrencyStamp = "3e9c1b7a-5d2f-4e8b-a6c4-9f1d3b7e5a20";
    public const string ManagerConcurrencyStamp = "a7d4e2b9-1c6f-4b3a-8e5d-2f9c7a1b4e63";

    // Pre-computed with PasswordHasher<IdentityUser>; regenerate if the seeded password changes.
    public const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEACetcoOnESG4t7RY1t7m00Dbs1jxpMIgT/84iO3FxIFdu8Abje3KDC4/TdYC+spow==";
    public const string ManagerPasswordHash =
        "AQAAAAIAAYagAAAAEP5tIlHRsgC+r8NLx0YX5CFjy9+WMfunvEaLpYWZBlNr91b/2SlvomIvmx5pP+H0PQ==";
}
```

- [ ] **Step 5: Criar `Data/Seed/RoleSeedConfiguration.cs`**

```csharp
using App.IdentityServer.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class RoleSeedConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder) => builder.HasData(
        new IdentityRole
        {
            Id = IdentitySeedData.AdminRoleId,
            Name = AppRoles.Admin,
            NormalizedName = AppRoles.Admin.ToUpperInvariant(),
            ConcurrencyStamp = IdentitySeedData.AdminRoleConcurrencyStamp
        },
        new IdentityRole
        {
            Id = IdentitySeedData.ManagerRoleId,
            Name = AppRoles.Manager,
            NormalizedName = AppRoles.Manager.ToUpperInvariant(),
            ConcurrencyStamp = IdentitySeedData.ManagerRoleConcurrencyStamp
        });
}
```

- [ ] **Step 6: Criar `Data/Seed/UserSeedConfiguration.cs`**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class UserSeedConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder) => builder.HasData(
        new IdentityUser
        {
            Id = IdentitySeedData.AdminUserId,
            UserName = "Admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@storehouse.local",
            NormalizedEmail = "ADMIN@STOREHOUSE.LOCAL",
            EmailConfirmed = true,
            PhoneNumber = "+351910000001",
            PhoneNumberConfirmed = true,
            PasswordHash = IdentitySeedData.AdminPasswordHash,
            SecurityStamp = IdentitySeedData.AdminSecurityStamp,
            ConcurrencyStamp = IdentitySeedData.AdminConcurrencyStamp,
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            AccessFailedCount = 0
        },
        new IdentityUser
        {
            Id = IdentitySeedData.ManagerUserId,
            UserName = "Manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@storehouse.local",
            NormalizedEmail = "MANAGER@STOREHOUSE.LOCAL",
            EmailConfirmed = true,
            PhoneNumber = "+351910000002",
            PhoneNumberConfirmed = true,
            PasswordHash = IdentitySeedData.ManagerPasswordHash,
            SecurityStamp = IdentitySeedData.ManagerSecurityStamp,
            ConcurrencyStamp = IdentitySeedData.ManagerConcurrencyStamp,
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            AccessFailedCount = 0
        });
}
```

- [ ] **Step 7: Criar `Data/Seed/UserRoleSeedConfiguration.cs`**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class UserRoleSeedConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder) => builder.HasData(
        new IdentityUserRole<string> { UserId = IdentitySeedData.AdminUserId, RoleId = IdentitySeedData.AdminRoleId },
        new IdentityUserRole<string> { UserId = IdentitySeedData.ManagerUserId, RoleId = IdentitySeedData.ManagerRoleId });
}
```

- [ ] **Step 8: Criar `Data/Seed/RoleClaimSeedConfiguration.cs`**

```csharp
using App.IdentityServer.Authorization;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class RoleClaimSeedConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> builder) => builder.HasData(
        new IdentityRoleClaim<string>
        {
            Id = 1,
            RoleId = IdentitySeedData.AdminRoleId,
            ClaimType = JwtClaimTypes.Role,
            ClaimValue = AppRoles.Admin
        },
        new IdentityRoleClaim<string>
        {
            Id = 2,
            RoleId = IdentitySeedData.ManagerRoleId,
            ClaimType = JwtClaimTypes.Role,
            ClaimValue = AppRoles.Manager
        });
}
```

- [ ] **Step 9: Substituir `Program.cs` (acrescenta Identity, EF Core e migração no arranque)**

```csharp
using App.IdentityServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouseIdentity");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'StoreHouseIdentity' is not configured.");
}

builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Identity", Version = "v1" }));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 10: Compilar**

Run: `dotnet build IdentityServer/App.IdentityServer`
Expected: `Build succeeded`.

- [ ] **Step 11: Instalar ou atualizar a ferramenta `dotnet-ef`**

Run:
```powershell
dotnet tool update --global dotnet-ef --version 10.0.13
dotnet ef --version
```
Expected: `Entity Framework Core .NET Command-line Tools 10.0.13`.

- [ ] **Step 12: Gerar a migration**

Run:
```powershell
dotnet ef migrations add InitialIdentity --project IdentityServer/App.IdentityServer --output-dir Data/Migrations
```
Expected: `Done.`, e a pasta `Data/Migrations` contém `*_InitialIdentity.cs`, `*_InitialIdentity.Designer.cs` e `AppIdentityDbContextModelSnapshot.cs`.

- [ ] **Step 13: Inspecionar a migration gerada**

Abrir `Data/Migrations/*_InitialIdentity.cs` e confirmar que existem:
- `migrationBuilder.InsertData(table: "AspNetRoles"` com `Admin` e `Manager`;
- `InsertData(table: "AspNetUsers"` com os campos `PasswordHash` (as hashes do Step 4) e `UserName` (`Admin` e `Manager`);
- `InsertData(table: "AspNetRoleClaims"` com `ClaimType` = `"role"`;
- `InsertData(table: "AspNetUserRoles"`.

Confirmar também que não aparecem as passwords em texto: procurar `nebur` no projeto não deve dar resultados.
```powershell
Select-String -Path IdentityServer/App.IdentityServer -Pattern "nebur" -Recurse -ErrorAction SilentlyContinue
```
Expected: sem resultados.

Se o `Select-String` não aceitar `-Recurse`, usar: `Get-ChildItem IdentityServer -Recurse -File | Select-String "nebur"`.

- [ ] **Step 14: Aplicar a migration à base local**

Run:
```powershell
dotnet ef database update --project IdentityServer/App.IdentityServer
dotnet ef migrations list --project IdentityServer/App.IdentityServer
```
Expected: `Done.`, e a lista mostra `..._InitialIdentity` sem a indicação `(Pending)`.

- [ ] **Step 15: Checkpoint**

Run: `dotnet run --project IdentityServer/App.IdentityServer --launch-profile https`
Expected: arranca sem erros (a migração no arranque não tem nada a aplicar) e o Swagger abre. Parar com `Ctrl+C`.

---

### Task 3: Duende IdentityServer e endpoint `POST /api/auth/login`

Entregável: o login com `Admin` ou `Manager` devolve um access token JWT com `iss = https://localhost:7001`, `aud = product.api`, `scope = product.api`, `name` e `role`. Credenciais erradas devolvem `401`, e um pedido inválido devolve `400`.

**Files:**
- Create: `IdentityServer/App.IdentityServer/Configuration/IdentityServerSettings.cs`
- Create: `IdentityServer/App.IdentityServer/Configuration/IdentityServerConfig.cs`
- Create: `IdentityServer/App.IdentityServer/Services/DistinctClaimsProfileService.cs`
- Create: `IdentityServer/App.IdentityServer/DTOs/LoginRequestDTO.cs`
- Create: `IdentityServer/App.IdentityServer/DTOs/TokenResponseDTO.cs`
- Create: `IdentityServer/App.IdentityServer/Controllers/AuthController.cs`
- Modify: `IdentityServer/App.IdentityServer/Program.cs` (substituir na totalidade)
- Modify: `IdentityServer/App.IdentityServer/appsettings.json` (substituir na totalidade)

**Interfaces:**
- Consumes: `AppIdentityDbContext` e `AppRoles` da Task 2.
- Produces, usados pelos Planos 02 e 03:
  - endpoint `POST https://localhost:7001/api/auth/login`, com body `{ "userName": "...", "password": "..." }` e resposta `{ "accessToken": "...", "tokenType": "Bearer", "expiresIn": 3600 }`;
  - JWT com `iss = https://localhost:7001`, `aud = product.api`, claims `role` e `name`;
  - discovery em `/.well-known/openid-configuration`;
  - variáveis de configuração `IdentityServer:IssuerUri`, `IdentityServer:TokenEndpoint`, `IdentityServer:ClientId`, `IdentityServer:ClientSecret`, `IdentityServer:ApiScope`, `IdentityServer:AccessTokenLifetimeSeconds` e `IdentityServer:KeyPath`.

- [ ] **Step 1: Configurar o client secret em User Secrets**

Run:
```powershell
$secret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "IdentityServer:ClientSecret" $secret --project IdentityServer/App.IdentityServer
```
Expected: `Successfully saved IdentityServer:ClientSecret to the secret store.`

- [ ] **Step 2: Substituir `appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IdentityServer": {
    "IssuerUri": "https://localhost:7001",
    "TokenEndpoint": "https://localhost:7001/connect/token",
    "ClientId": "storehouse.swagger",
    "ApiScope": "product.api",
    "AccessTokenLifetimeSeconds": 3600
  }
}
```

- [ ] **Step 3: Criar `Configuration/IdentityServerSettings.cs`**

```csharp
namespace App.IdentityServer.Configuration;

public sealed class IdentityServerSettings
{
    public const string SectionName = "IdentityServer";

    public string IssuerUri { get; init; } = string.Empty;
    public string TokenEndpoint { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string ApiScope { get; init; } = string.Empty;
    public int AccessTokenLifetimeSeconds { get; init; } = 3600;
    public string? KeyPath { get; init; }

    public void Validate()
    {
        (string Name, string Value)[] requiredValues =
        [
            (nameof(IssuerUri), IssuerUri),
            (nameof(TokenEndpoint), TokenEndpoint),
            (nameof(ClientId), ClientId),
            (nameof(ClientSecret), ClientSecret),
            (nameof(ApiScope), ApiScope)
        ];

        foreach (var (name, value) in requiredValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration value '{SectionName}:{name}' is required.");
            }
        }
    }
}
```

- [ ] **Step 4: Criar `Configuration/IdentityServerConfig.cs`**

```csharp
using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace App.IdentityServer.Configuration;

public static class IdentityServerConfig
{
    public static IEnumerable<ApiScope> GetApiScopes(IdentityServerSettings settings) =>
        [new ApiScope(settings.ApiScope, "StoreHouse Product API")];

    public static IEnumerable<ApiResource> GetApiResources(IdentityServerSettings settings) =>
    [
        new ApiResource(settings.ApiScope, "StoreHouse Product API")
        {
            Scopes = { settings.ApiScope },
            UserClaims = { JwtClaimTypes.Role, JwtClaimTypes.Name }
        }
    ];

    public static IEnumerable<Client> GetClients(IdentityServerSettings settings) =>
    [
        new Client
        {
            ClientId = settings.ClientId,
            ClientName = "StoreHouse Swagger",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Secret(settings.ClientSecret.Sha256()) },
            AllowedScopes = { settings.ApiScope },
            AccessTokenLifetime = settings.AccessTokenLifetimeSeconds,
            AllowOfflineAccess = false
        }
    ];
}
```

- [ ] **Step 5: Criar `Services/DistinctClaimsProfileService.cs`**

O `UserClaimsPrincipalFactory` acrescenta a claim `role` duas vezes: uma vinda do nome da role e outra vinda da role claim do seed. Este serviço remove o duplicado antes de emitir o token.

```csharp
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

namespace App.IdentityServer.Services;

public sealed class DistinctClaimsProfileService(
    UserManager<IdentityUser> userManager,
    IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
    ILogger<ProfileService<IdentityUser>> logger)
    : ProfileService<IdentityUser>(userManager, claimsFactory, logger)
{
    public override async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        await base.GetProfileDataAsync(context, ct);
        context.IssuedClaims = context.IssuedClaims
            .DistinctBy(claim => (claim.Type, claim.Value))
            .ToList();
    }
}
```

- [ ] **Step 6: Criar os DTOs**

`DTOs/LoginRequestDTO.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace App.IdentityServer.DTOs;

public sealed record LoginRequestDTO(
    [Required, StringLength(256)] string UserName,
    [Required, StringLength(128)] string Password);
```

`DTOs/TokenResponseDTO.cs`:
```csharp
namespace App.IdentityServer.DTOs;

public sealed record TokenResponseDTO(string AccessToken, string TokenType, int ExpiresIn);
```

- [ ] **Step 7: Criar `Controllers/AuthController.cs`**

```csharp
using App.IdentityServer.Configuration;
using App.IdentityServer.DTOs;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace App.IdentityServer.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IHttpClientFactory httpClientFactory,
    IOptions<IdentityServerSettings> options,
    ILogger<AuthController> logger) : ControllerBase
{
    public const string TokenClientName = "IdentityServerTokenClient";

    [HttpPost("login")]
    [ProducesResponseType<TokenResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var httpClient = httpClientFactory.CreateClient(TokenClientName);

        using var tokenRequest = new PasswordTokenRequest
        {
            Address = settings.TokenEndpoint,
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret,
            Scope = settings.ApiScope,
            UserName = request.UserName,
            Password = request.Password
        };

        using var tokenResponse = await httpClient.RequestPasswordTokenAsync(tokenRequest, cancellationToken);

        if (!tokenResponse.IsError)
        {
            return Ok(new TokenResponseDTO(tokenResponse.AccessToken!, tokenResponse.TokenType!, tokenResponse.ExpiresIn));
        }

        if (tokenResponse.Error == OidcConstants.TokenErrors.InvalidGrant)
        {
            logger.LogWarning("Failed login attempt for user {UserName}", request.UserName);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The user name or password is incorrect.");
        }

        throw new InvalidOperationException(
            $"Token endpoint returned '{tokenResponse.Error}': {tokenResponse.ErrorDescription}");
    }
}
```

- [ ] **Step 8: Substituir `Program.cs` (versão final)**

O IdentityServer não usa `UseHttpsRedirection`. No Docker, a Product.Api lê o discovery por HTTP interno, e o redirecionamento partiria essa chamada. O acesso público continua a ser feito só por HTTPS (porta 7001).

```csharp
using App.IdentityServer.Configuration;
using App.IdentityServer.Controllers;
using App.IdentityServer.Data;
using App.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouseIdentity");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'StoreHouseIdentity' is not configured.");
}

var identityServerSection = builder.Configuration.GetSection(IdentityServerSettings.SectionName);
var identityServerSettings = identityServerSection.Get<IdentityServerSettings>()
    ?? throw new InvalidOperationException($"Configuration section '{IdentityServerSettings.SectionName}' is missing.");
identityServerSettings.Validate();
builder.Services.Configure<IdentityServerSettings>(identityServerSection);

builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = identityServerSettings.IssuerUri;
        options.EmitStaticAudienceClaim = false;
        if (!string.IsNullOrWhiteSpace(identityServerSettings.KeyPath))
        {
            options.KeyManagement.KeyPath = identityServerSettings.KeyPath;
        }
    })
    .AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes(identityServerSettings))
    .AddInMemoryApiResources(IdentityServerConfig.GetApiResources(identityServerSettings))
    .AddInMemoryClients(IdentityServerConfig.GetClients(identityServerSettings))
    .AddAspNetIdentity<IdentityUser>()
    .AddProfileService<DistinctClaimsProfileService>();

builder.Services.AddHttpClient(AuthController.TokenClientName);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Identity", Version = "v1" }));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIdentityServer();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 9: Compilar**

Run: `dotnet build IdentityServer/App.IdentityServer`
Expected: `Build succeeded` com 0 erros.

- [ ] **Step 10: Arrancar o IdentityServer num terminal dedicado**

Run (terminal 1): `dotnet run --project IdentityServer/App.IdentityServer --launch-profile https`
Expected:
- `Now listening on: https://localhost:7001`;
- avisos do Duende sobre a ausência de licença (esperado em desenvolvimento);
- o Swagger mostra apenas `POST /api/auth/login`.

- [ ] **Step 11: Verificar o login do Admin e o conteúdo do token**

Run (terminal 2):
```powershell
$body = @{ userName = 'Admin'; password = 'nebur#123' } | ConvertTo-Json
$response = Invoke-RestMethod -Method Post -Uri https://localhost:7001/api/auth/login -ContentType 'application/json' -Body $body
$payload = $response.accessToken.Split('.')[1].Replace('-', '+').Replace('_', '/')
$payload += '=' * ((4 - $payload.Length % 4) % 4)
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload))
$response.tokenType; $response.expiresIn
```
Expected:
- o JSON do payload contém `"iss":"https://localhost:7001"`, `"aud":"product.api"`, `"role":"Admin"` (um valor único, não um array), `"name":"Admin"` e `"scope":["product.api"]`;
- `tokenType` = `Bearer` e `expiresIn` = `3600`.

- [ ] **Step 12: Verificar o login do Manager**

Repetir o Step 11 com `userName = 'Manager'` e `password = 'nebur2#123'`.
Expected: `"role":"Manager"` e `"name":"Manager"`.

- [ ] **Step 13: Verificar credenciais inválidas (401) e pedido inválido (400)**

Run:
```powershell
function Get-Status([string]$json) {
    try { Invoke-WebRequest -Method Post -Uri https://localhost:7001/api/auth/login -ContentType 'application/json' -Body $json -UseBasicParsing | Out-Null; 200 }
    catch { [int]$_.Exception.Response.StatusCode }
}
Get-Status (@{ userName = 'Admin'; password = 'errada' } | ConvertTo-Json)
Get-Status (@{ userName = ''; password = '' } | ConvertTo-Json)
```
Expected: `401` e depois `400`.

- [ ] **Step 14: Checkpoint final do Plano 01**

Run: `dotnet build StoreHouse.slnx`
Expected: `Build succeeded`.

O IdentityServer está pronto e o Plano 02 depende dele. Parar com `Ctrl+C`.

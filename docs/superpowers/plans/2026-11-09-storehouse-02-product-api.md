# StoreHouse — Plano 02: Product.Api (Clean Architecture + CQRS) e Product.Api.UnitTest

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar a API de produtos e stock com os 9 endpoints do enunciado, protegida pelos tokens do App.IdentityServer, e o projeto de testes unitários `Product.Api.UnitTest`.

**Architecture:** Clean Architecture em 4 projetos: Domain, Application, Infrastructure e Api.
- **Application:** organizada por feature, com CQRS via MediatR (commands e queries), FluentValidation num pipeline behavior, padrão Result para os erros esperados e Mapster para os DTOs.
- **Infrastructure:** EF Core com Npgsql, DbContext pooling, UnitOfWork com execution strategy, `UPDATE` atómico para o stock, `xmin` para concorrência otimista e soft delete com query filter.
- **Api:** controllers finos, JWT Bearer, Swagger com Authorize e um handler global de exceções.

**Tech Stack:** .NET 10, C# 14, MediatR 12.5.0, Mapster 10.0.14, FluentValidation 12.2.0, EF Core 10.0.13, Npgsql EF 10.0.3, Swashbuckle 10.3.1, xUnit v3 3.2.2, Moq 4.20.72, FakeTimeProvider.

**Spec:** `docs/superpowers/specs/2026-11-08-storehouse-design.md`. A secção 10 do spec prevalece sobre as anteriores.

**Pré-requisito:** Plano 01 concluído. A solução, o `Directory.*.props` e o App.IdentityServer já existem, e o login funciona.

## Global Constraints

- Raiz: `C:\Users\Ruben Batista\OneDrive\Documents\Projetos\StoreHouse` (`StoreHouse/`). Todos os comandos correm a partir daqui.
- .NET 10 e C# 14, herdados do `Directory.Build.props`. Versões de pacotes só no `Directory.Packages.props`; os `.csproj` não têm `Version`.
- **Sem git:** os passos de "commit" são checkpoints de `dotnet build` e `dotnet test`.
- **Nomes:**
  - Inglês para código, configuração e base de dados.
  - Dados de seed em português.
  - DTOs terminam em `DTO`.
  - Nomes dos métodos dos controllers: `GetProducts`, `GetProductById`, `CreateProduct`, `UpdateProduct`, `DeleteProduct`, `DecreaseStockToProduct`, `AddStockToProduct`, `SearchProductsByName` e `SearchProductsByStockRange`.
- **Alias obrigatório:** fora do Domain, a entidade é sempre `ProductEntity`, através de `global using ProductEntity = Product.Domain.Entities.Product;`. Dentro de namespaces `Product.*`, o nome `Product` resolve para o namespace e não para a classe.
- **Camadas:** a Application e o Domain não referenciam ASP.NET Core nem EF Core. A Infrastructure não depende de `IConfiguration`.
- **Erros:**
  - O padrão Result é só para resultados esperados: `Validation` → 400, `NotFound` → 404, `Conflict` → 409, `BusinessRule` → 422.
  - Exceções inesperadas vão para o `GlobalExceptionHandler` (500).
  - Não há try/catch para logging.
- **Autorização:**
  - `POST /api/products` e `DELETE` exigem `[Authorize(Roles = "Admin")]`.
  - `PUT` e os dois endpoints de stock exigem `[Authorize]` sem roles.
  - Os GET são públicos.
- **Base de dados:** `StoreHouse`, com as tabelas `Product`, `Stock` e `StockMovement`. Os IDs vêm da sequence `ProductIdSequence` (100000–999999, começa em 100011).
- **Portas locais:** Product.Api em `https://localhost:7039` e `http://localhost:5039`; IdentityServer em `https://localhost:7001`.
- **Testes:**
  - correm com `dotnet test UnitTests/Product.Api.UnitTest`;
  - `CancellationToken` vem de `TestContext.Current.CancellationToken`;
  - sem FluentAssertions; usar `Assert` do xUnit.

## Mapa de ficheiros

```
Product/Product.Domain/
  Product.Domain.csproj
  Constants/ProductConstraints.cs
  Enums/StockMovementType.cs
  Entities/Product.cs, Stock.cs, StockMovement.cs
Product/Product.Application/
  Product.Application.csproj, GlobalUsings.cs, DependencyInjection.cs
  Common/Results/ErrorType.cs, Error.cs, IResultBase.cs, Result.cs
  Common/DTOs/PagedResultDTO.cs
  Common/Abstractions/IUnitOfWork.cs
  Common/Exceptions/ConcurrencyConflictException.cs
  Common/Behaviors/LoggingBehavior.cs, ValidationBehavior.cs
  Common/Logging/LogMessages.cs
  Products/ProductErrors.cs
  Products/DTOs/ProductDTO.cs, CreateProductDTO.cs, UpdateProductDTO.cs, StockDTO.cs
  Products/Abstractions/IProductRepository.cs, IProductQueries.cs, ProductFilter.cs
  Products/Mapping/ProductMappingConfig.cs
  Products/Validation/ProductRules.cs
  Products/Commands/{CreateProduct,UpdateProduct,DeleteProduct,AddStockToProduct,DecreaseStockToProduct}/
      <Name>Command.cs, <Name>CommandHandler.cs, <Name>CommandValidator.cs
  Products/Queries/{GetProducts,GetProductById,SearchProductsByName,SearchProductsByStockRange}/
      <Name>Query.cs, <Name>QueryHandler.cs, <Name>QueryValidator.cs
Product/Product.Infrastructure/
  Product.Infrastructure.csproj, GlobalUsings.cs, DependencyInjection.cs
  Persistence/ProductDbContext.cs, UnitOfWork.cs, LikePatternEscaper.cs
  Persistence/Configurations/ProductConfiguration.cs, StockConfiguration.cs, StockMovementConfiguration.cs
  Persistence/Seed/ProductSeedData.cs
  Persistence/Migrations/* (gerado)
  Products/ProductRepository.cs, ProductQueries.cs
Product/Product.Api/
  Product.Api.csproj, Program.cs, appsettings.json, appsettings.Development.json, Dockerfile
  Properties/launchSettings.json
  Authorization/AuthorizationRoles.cs
  Authentication/JwtSettings.cs, AuthenticationExtensions.cs
  Swagger/SwaggerExtensions.cs, AuthorizeOperationFilter.cs
  ErrorHandling/GlobalExceptionHandler.cs
  Extensions/ResultExtensions.cs
  Controllers/ProductsController.cs
UnitTests/Product.Api.UnitTest/
  Product.Api.UnitTest.csproj, GlobalUsings.cs
  TestData/TestProducts.cs, TestMapping.cs
  Common/ResultTests.cs
  Validators/*Tests.cs
  Behaviors/ValidationBehaviorTests.cs, LoggingBehaviorTests.cs
  Application/ProductMappingConfigTests.cs, ApplicationDependencyInjectionTests.cs
  Handlers/Queries/*Tests.cs, Handlers/Commands/*Tests.cs
  Infrastructure/LikePatternEscaperTests.cs
  Api/ResultExtensionsTests.cs, ProductsControllerTests.cs, ProductsControllerAuthorizationTests.cs, GlobalExceptionHandlerTests.cs
scripts/smoke-test.ps1
```

---

### Task 1: Projetos, referências, Domain e esqueleto da Api

Entregável: a solução compila com os 5 projetos novos e o Domain completo.

**Files:**
- Create: `Product/Product.Domain/Product.Domain.csproj`, `Constants/ProductConstraints.cs`, `Enums/StockMovementType.cs`, `Entities/Product.cs`, `Entities/Stock.cs`, `Entities/StockMovement.cs`
- Create: `Product/Product.Application/Product.Application.csproj`, `GlobalUsings.cs`
- Create: `Product/Product.Infrastructure/Product.Infrastructure.csproj`, `GlobalUsings.cs`
- Create: `Product/Product.Api/Product.Api.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`
- Create: `UnitTests/Product.Api.UnitTest/Product.Api.UnitTest.csproj`, `GlobalUsings.cs`
- Modify: `StoreHouse.slnx` (via `dotnet sln add`)

**Interfaces:**
- Produces:
  - `Product.Domain.Entities.Product`, com as propriedades `Id`, `Name`, `Description`, `Category`, `Price`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `DeletedBy`, `Version (uint)`, `Stock`, `StockMovements`;
  - `Stock`, com `ProductId`, `Quantity`, `UpdatedAt`, `Version`, `Product`;
  - `StockMovement`, com `Id (long)`, `ProductId`, `Type`, `Quantity`, `QuantityAfter`, `CreatedAt`, `CreatedBy`, `Product`;
  - `StockMovementType : short { Initial = 1, Increase = 2, Decrease = 3 }`;
  - as constantes de `ProductConstraints`.

- [ ] **Step 1: `Product/Product.Domain/Product.Domain.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

- [ ] **Step 2: `Product/Product.Domain/Constants/ProductConstraints.cs`**

```csharp
namespace Product.Domain.Constants;

public static class ProductConstraints
{
    public const int MinId = 100_000;
    public const int MaxId = 999_999;
    public const int NameMaxLength = 100;
    public const int NameSearchMinLength = 2;
    public const int DescriptionMaxLength = 500;
    public const int CategoryMaxLength = 50;
    public const int UserNameMaxLength = 100;
    public const int PricePrecision = 10;
    public const int PriceScale = 2;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 99_999_999.99m;
    public const int MaxQuantityPerOperation = 1_000_000;
    public const int DefaultPageSize = 5;
    public const int MaxPageSize = 50;
}
```

- [ ] **Step 3: `Product/Product.Domain/Enums/StockMovementType.cs`**

```csharp
namespace Product.Domain.Enums;

public enum StockMovementType : short
{
    Initial = 1,
    Increase = 2,
    Decrease = 3
}
```

- [ ] **Step 4: `Product/Product.Domain/Entities/Product.cs`**

```csharp
namespace Product.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public uint Version { get; set; }
    public Stock? Stock { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
```

- [ ] **Step 5: `Product/Product.Domain/Entities/Stock.cs`**

```csharp
namespace Product.Domain.Entities;

public class Stock
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint Version { get; set; }
    public Product? Product { get; set; }
}
```

- [ ] **Step 6: `Product/Product.Domain/Entities/StockMovement.cs`**

```csharp
using Product.Domain.Enums;

namespace Product.Domain.Entities;

public class StockMovement
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public int QuantityAfter { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public Product? Product { get; set; }
}
```

- [ ] **Step 7: `Product/Product.Application/Product.Application.csproj` e `GlobalUsings.cs`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Mapster" />
    <PackageReference Include="MediatR" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Product.Domain\Product.Domain.csproj" />
  </ItemGroup>

</Project>
```

`Product/Product.Application/GlobalUsings.cs`:
```csharp
global using ProductEntity = Product.Domain.Entities.Product;
```

- [ ] **Step 8: `Product/Product.Infrastructure/Product.Infrastructure.csproj` e `GlobalUsings.cs`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Product.Application\Product.Application.csproj" />
    <ProjectReference Include="..\Product.Domain\Product.Domain.csproj" />
  </ItemGroup>

</Project>
```

`Product/Product.Infrastructure/GlobalUsings.cs`:
```csharp
global using ProductEntity = Product.Domain.Entities.Product;
```

- [ ] **Step 9: `Product/Product.Api/Product.Api.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>e2a4c6d8-1b3f-4a5c-8e7d-9f0a1b2c3d4e</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..</DockerfileContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Product.Application\Product.Application.csproj" />
    <ProjectReference Include="..\Product.Infrastructure\Product.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 10: `Product/Product.Api/Program.cs` (esqueleto)**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 11: `appsettings.json`, `appsettings.Development.json` e `Properties/launchSettings.json` da Api**

`Product/Product.Api/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`Product/Product.Api/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

`Product/Product.Api/Properties/launchSettings.json`:
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7039;http://localhost:5039",
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

- [ ] **Step 12: `UnitTests/Product.Api.UnitTest/Product.Api.UnitTest.csproj` e `GlobalUsings.cs`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Moq" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Product\Product.Api\Product.Api.csproj" />
    <ProjectReference Include="..\..\Product\Product.Application\Product.Application.csproj" />
  </ItemGroup>

</Project>
```

`UnitTests/Product.Api.UnitTest/GlobalUsings.cs`:
```csharp
global using ProductEntity = Product.Domain.Entities.Product;
```

- [ ] **Step 13: Adicionar os projetos à solução, nas pastas `Product` e `UnitTests`**

Run:
```powershell
dotnet sln StoreHouse.slnx add Product/Product.Domain/Product.Domain.csproj Product/Product.Application/Product.Application.csproj Product/Product.Infrastructure/Product.Infrastructure.csproj Product/Product.Api/Product.Api.csproj --solution-folder Product
dotnet sln StoreHouse.slnx add UnitTests/Product.Api.UnitTest/Product.Api.UnitTest.csproj --solution-folder UnitTests
dotnet sln StoreHouse.slnx list
```
Expected: a lista mostra os 6 projetos.

- [ ] **Step 14: Checkpoint**

Run: `dotnet build StoreHouse.slnx`
Expected: `Build succeeded` com 0 erros.

---

### Task 2: Padrão Result

Entregável: os tipos `Result`, `Result<T>` e `Error`, testados.

**Files:**
- Create: `Product/Product.Application/Common/Results/ErrorType.cs`, `Error.cs`, `IResultBase.cs`, `Result.cs`
- Test: `UnitTests/Product.Api.UnitTest/Common/ResultTests.cs`

**Interfaces:**
- Produces:
  - `enum ErrorType { Validation, NotFound, Conflict, BusinessRule }`;
  - `sealed record Error(string Code, string Message, ErrorType Type)`, com a propriedade `IReadOnlyDictionary<string, string[]> ValidationErrors` e o método `static Error Validation(IReadOnlyDictionary<string, string[]>)`;
  - `interface IResultBase<TSelf>`, com `bool IsSuccess`, `Error? Error` e `static abstract TSelf Failure(Error)`;
  - `sealed class Result`, com `Success()`, `Failure(Error)` e conversão implícita a partir de `Error`;
  - `sealed class Result<T>`, com `Value`, `Success(T)`, `Failure(Error)` e conversões implícitas a partir de `T` e de `Error`;
  - `IsFailure` em ambos.

- [ ] **Step 1: Escrever o teste `UnitTests/Product.Api.UnitTest/Common/ResultTests.cs`**

```csharp
using Product.Application.Common.Results;

namespace Product.Api.UnitTest.Common;

public sealed class ResultTests
{
    private static readonly Error NotFoundError = new("Product.NotFound", "Not found.", ErrorType.NotFound);

    [Fact]
    public void Success_IsSuccessWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ExposesError()
    {
        var result = Result.Failure(NotFoundError);

        Assert.True(result.IsFailure);
        Assert.Same(NotFoundError, result.Error);
    }

    [Fact]
    public void Failure_WithNullError_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenValueIsRead()
    {
        Result<string> result = NotFoundError;

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ValidationError_CarriesFieldErrors()
    {
        var error = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["'Name' must not be empty."] });

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("Validation.Failed", error.Code);
        Assert.Equal(["'Name' must not be empty."], error.ValidationErrors["Name"]);
    }

    [Fact]
    public void StaticAbstractFailure_CreatesFailureOfTheRequestedType()
    {
        var generic = CreateFailure<Result<int>>(NotFoundError);
        var nonGeneric = CreateFailure<Result>(NotFoundError);

        Assert.True(generic.IsFailure);
        Assert.True(nonGeneric.IsFailure);
    }

    private static TResult CreateFailure<TResult>(Error error) where TResult : IResultBase<TResult> =>
        TResult.Failure(error);
}
```

- [ ] **Step 2: Correr o teste e confirmar que falha**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~ResultTests"`
Expected: FAIL na compilação, com `CS0246: The type or namespace name 'Result' could not be found` (ou `Error`, `ErrorType`, `IResultBase`).

- [ ] **Step 3: Implementar os tipos**

`Product/Product.Application/Common/Results/ErrorType.cs`:
```csharp
namespace Product.Application.Common.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule
}
```

`Product/Product.Application/Common/Results/Error.cs`:
```csharp
namespace Product.Application.Common.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = new Dictionary<string, string[]>();

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation)
        {
            ValidationErrors = validationErrors
        };
}
```

`Product/Product.Application/Common/Results/IResultBase.cs`:
```csharp
namespace Product.Application.Common.Results;

public interface IResultBase<TSelf> where TSelf : IResultBase<TSelf>
{
    bool IsSuccess { get; }

    Error? Error { get; }

    static abstract TSelf Failure(Error error);
}
```

`Product/Product.Application/Common/Results/Result.cs`:
```csharp
namespace Product.Application.Common.Results;

public sealed class Result : IResultBase<Result>
{
    private Result(Error? error) => Error = error;

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : IResultBase<Result<T>>
{
    private readonly T? _value;

    private Result(T value) => _value = value;

    private Result(Error error) => Error = error;

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

- [ ] **Step 4: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~ResultTests"`
Expected: PASS, 7 testes.

- [ ] **Step 5: Checkpoint**

Run: `dotnet build StoreHouse.slnx`
Expected: `Build succeeded`.

---

### Task 3: DTOs, pedidos CQRS e validadores FluentValidation

Entregável: todos os DTOs, commands e queries (os records MediatR) e os 9 validadores, testados nos limites de cada regra.

**Files:**
- Create: `Product/Product.Application/Common/DTOs/PagedResultDTO.cs`
- Create: `Product/Product.Application/Products/DTOs/ProductDTO.cs`, `CreateProductDTO.cs`, `UpdateProductDTO.cs`, `StockDTO.cs`
- Create: `Product/Product.Application/Products/Validation/ProductRules.cs`
- Create: os `*Command.cs`, `*CommandValidator.cs`, `*Query.cs` e `*QueryValidator.cs` das 9 operações
- Test: `UnitTests/Product.Api.UnitTest/Validators/CreateProductCommandValidatorTests.cs`, `UpdateProductCommandValidatorTests.cs`, `IdAndStockCommandValidatorsTests.cs`, `QueryValidatorsTests.cs`

**Interfaces:**
- Consumes: `Result`, `Result<T>` (Task 2) e `ProductConstraints` (Task 1).
- Produces:
  - DTOs:
    - `PagedResultDTO<T>(IReadOnlyList<T> Items, int? NextCursor, bool HasMore)`;
    - `ProductDTO(int Id, string Name, string? Description, string Category, decimal Price, int StockQuantity)`;
    - `CreateProductDTO(string Name, string? Description, string Category, decimal Price, int InitialStock)`;
    - `UpdateProductDTO(string Name, string? Description, string Category, decimal Price)`;
    - `StockDTO(int ProductId, int Quantity)`.
  - Commands:
    - `CreateProductCommand(CreateProductDTO Product, string? UserName) : IRequest<Result<ProductDTO>>`;
    - `UpdateProductCommand(int Id, UpdateProductDTO Product) : IRequest<Result<ProductDTO>>`;
    - `DeleteProductCommand(int Id, string? UserName) : IRequest<Result>`;
    - `AddStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>`;
    - `DecreaseStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>`.
  - Queries:
    - `GetProductsQuery(int? LastItemId, int PageSize = 5) : IRequest<Result<PagedResultDTO<ProductDTO>>>`;
    - `GetProductByIdQuery(int Id) : IRequest<Result<ProductDTO>>`;
    - `SearchProductsByNameQuery(string Name, int? LastItemId, int PageSize = 5)`, com a mesma resposta paginada;
    - `SearchProductsByStockRangeQuery(int Min, int Max, int? LastItemId, int PageSize = 5)`, com a mesma resposta paginada.
  - Validadores: `<Request>Validator : AbstractValidator<Request>`, um por cada pedido.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Validators/CreateProductCommandValidatorTests.cs`**

```csharp
using FluentValidation.TestHelper;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Validators;

public sealed class CreateProductCommandValidatorTests
{
    private static readonly CreateProductDTO ValidProduct =
        new("Martelo de Carpinteiro", "Cabo de madeira.", "Ferramentas Manuais", 12.90m, 10);

    private readonly CreateProductCommandValidator _validator = new();

    public static TheoryData<decimal> InvalidPrices => new() { 0m, -1m, 0.001m, 100_000_000m };

    public static TheoryData<decimal> ValidPrices => new() { 0.01m, 99_999_999.99m };

    private static CreateProductCommand Command(CreateProductDTO product) => new(product, "Admin");

    [Fact]
    public void ValidCommand_HasNoErrors() =>
        _validator.TestValidate(Command(ValidProduct)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullProduct_Fails() =>
        _validator.TestValidate(new CreateProductCommand(null!, "Admin")).ShouldHaveValidationErrorFor(x => x.Product);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_Fails(string name) =>
        _validator.TestValidate(Command(ValidProduct with { Name = name })).ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void NameAboveMaxLength_Fails() =>
        _validator.TestValidate(Command(ValidProduct with { Name = new string('a', 101) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void NameAtMaxLength_Passes() =>
        _validator.TestValidate(Command(ValidProduct with { Name = new string('a', 100) }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void DescriptionAboveMaxLength_Fails() =>
        _validator.TestValidate(Command(ValidProduct with { Description = new string('a', 501) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Description);

    [Fact]
    public void NullDescription_Passes() =>
        _validator.TestValidate(Command(ValidProduct with { Description = null }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Description);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyCategory_Fails(string category) =>
        _validator.TestValidate(Command(ValidProduct with { Category = category }))
            .ShouldHaveValidationErrorFor(x => x.Product.Category);

    [Fact]
    public void CategoryAboveMaxLength_Fails() =>
        _validator.TestValidate(Command(ValidProduct with { Category = new string('a', 51) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Category);

    [Theory]
    [MemberData(nameof(InvalidPrices))]
    public void InvalidPrice_Fails(decimal price) =>
        _validator.TestValidate(Command(ValidProduct with { Price = price }))
            .ShouldHaveValidationErrorFor(x => x.Product.Price);

    [Theory]
    [MemberData(nameof(ValidPrices))]
    public void ValidPrice_Passes(decimal price) =>
        _validator.TestValidate(Command(ValidProduct with { Price = price }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Price);

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void InvalidInitialStock_Fails(int initialStock) =>
        _validator.TestValidate(Command(ValidProduct with { InitialStock = initialStock }))
            .ShouldHaveValidationErrorFor(x => x.Product.InitialStock);

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_000)]
    public void ValidInitialStock_Passes(int initialStock) =>
        _validator.TestValidate(Command(ValidProduct with { InitialStock = initialStock }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.InitialStock);
}
```

- [ ] **Step 2: Escrever `UnitTests/Product.Api.UnitTest/Validators/UpdateProductCommandValidatorTests.cs`**

```csharp
using FluentValidation.TestHelper;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Validators;

public sealed class UpdateProductCommandValidatorTests
{
    private static readonly UpdateProductDTO ValidProduct = new("Martelo", null, "Ferramentas Manuais", 12.90m);

    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void IdOutsideRange_Fails(int id) =>
        _validator.TestValidate(new UpdateProductCommand(id, ValidProduct)).ShouldHaveValidationErrorFor(x => x.Id);

    [Theory]
    [InlineData(100_000)]
    [InlineData(999_999)]
    public void IdAtLimits_Passes(int id) =>
        _validator.TestValidate(new UpdateProductCommand(id, ValidProduct)).ShouldNotHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct with { Name = "" }))
            .ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void ZeroPrice_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct with { Price = 0m }))
            .ShouldHaveValidationErrorFor(x => x.Product.Price);

    [Fact]
    public void CategoryAboveMaxLength_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct with { Category = new string('a', 51) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Category);

    [Fact]
    public void NullProduct_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, null!)).ShouldHaveValidationErrorFor(x => x.Product);
}
```

- [ ] **Step 3: Escrever `UnitTests/Product.Api.UnitTest/Validators/IdAndStockCommandValidatorsTests.cs`**

```csharp
using FluentValidation.TestHelper;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;

namespace Product.Api.UnitTest.Validators;

public sealed class IdAndStockCommandValidatorsTests
{
    private readonly DeleteProductCommandValidator _deleteValidator = new();
    private readonly AddStockToProductCommandValidator _addValidator = new();
    private readonly DecreaseStockToProductCommandValidator _decreaseValidator = new();

    [Theory]
    [InlineData(5)]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void InvalidId_Fails(int id)
    {
        _deleteValidator.TestValidate(new DeleteProductCommand(id, "Admin")).ShouldHaveValidationErrorFor(x => x.Id);
        _addValidator.TestValidate(new AddStockToProductCommand(id, 1, "Manager")).ShouldHaveValidationErrorFor(x => x.Id);
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(id, 1, "Manager")).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void ValidDelete_HasNoErrors() =>
        _deleteValidator.TestValidate(new DeleteProductCommand(100001, "Admin")).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_001)]
    public void InvalidQuantity_Fails(int quantity)
    {
        _addValidator.TestValidate(new AddStockToProductCommand(100001, quantity, "Manager"))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(100001, quantity, "Manager"))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void ValidQuantity_Passes(int quantity)
    {
        _addValidator.TestValidate(new AddStockToProductCommand(100001, quantity, "Manager"))
            .ShouldNotHaveAnyValidationErrors();
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(100001, quantity, "Manager"))
            .ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 4: Escrever `UnitTests/Product.Api.UnitTest/Validators/QueryValidatorsTests.cs`**

```csharp
using FluentValidation.TestHelper;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Validators;

public sealed class QueryValidatorsTests
{
    private readonly GetProductsQueryValidator _getProductsValidator = new();
    private readonly GetProductByIdQueryValidator _getByIdValidator = new();
    private readonly SearchProductsByNameQueryValidator _searchByNameValidator = new();
    private readonly SearchProductsByStockRangeQueryValidator _stockRangeValidator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void GetProducts_InvalidPageSize_Fails(int pageSize) =>
        _getProductsValidator.TestValidate(new GetProductsQuery(null, pageSize)).ShouldHaveValidationErrorFor(x => x.PageSize);

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void GetProducts_ValidPageSize_Passes(int pageSize) =>
        _getProductsValidator.TestValidate(new GetProductsQuery(null, pageSize)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GetProducts_InvalidCursor_Fails() =>
        _getProductsValidator.TestValidate(new GetProductsQuery(99_999, 5)).ShouldHaveValidationErrorFor(x => x.LastItemId);

    [Fact]
    public void GetProducts_ValidCursor_Passes() =>
        _getProductsValidator.TestValidate(new GetProductsQuery(100_005, 5)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GetProductById_InvalidId_Fails() =>
        _getByIdValidator.TestValidate(new GetProductByIdQuery(99_999)).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void GetProductById_ValidId_Passes() =>
        _getByIdValidator.TestValidate(new GetProductByIdQuery(100_001)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" a ")]
    public void SearchByName_TooShort_Fails(string name) =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery(name, null, 5)).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void SearchByName_TooLong_Fails() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery(new string('a', 101), null, 5))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void SearchByName_Valid_Passes() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery("ma", null, 5)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void SearchByName_InvalidPageSize_Fails() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery("martelo", null, 0))
            .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void StockRange_NegativeMin_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(-1, 10, null, 5))
            .ShouldHaveValidationErrorFor(x => x.Min);

    [Fact]
    public void StockRange_MaxBelowMin_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(20, 10, null, 5))
            .ShouldHaveValidationErrorFor(x => x.Max);

    [Fact]
    public void StockRange_MinEqualToMax_Passes() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(10, 10, null, 5))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void StockRange_InvalidCursor_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(0, 10, 5, 5))
            .ShouldHaveValidationErrorFor(x => x.LastItemId);
}
```

- [ ] **Step 5: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Validators"`
Expected: FAIL na compilação (`CS0246` para `CreateProductCommandValidator`, `CreateProductDTO`, etc.).

- [ ] **Step 6: Criar os DTOs**

`Product/Product.Application/Common/DTOs/PagedResultDTO.cs`:
```csharp
namespace Product.Application.Common.DTOs;

public sealed record PagedResultDTO<T>(IReadOnlyList<T> Items, int? NextCursor, bool HasMore);
```

`Product/Product.Application/Products/DTOs/ProductDTO.cs`:
```csharp
namespace Product.Application.Products.DTOs;

public sealed record ProductDTO(
    int Id,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int StockQuantity);
```

`Product/Product.Application/Products/DTOs/CreateProductDTO.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Product.Domain.Constants;

namespace Product.Application.Products.DTOs;

public sealed record CreateProductDTO(
    [Required, StringLength(ProductConstraints.NameMaxLength)] string Name,
    [StringLength(ProductConstraints.DescriptionMaxLength)] string? Description,
    [Required, StringLength(ProductConstraints.CategoryMaxLength)] string Category,
    [Range(0.01, 99_999_999.99)] decimal Price,
    [Range(0, ProductConstraints.MaxQuantityPerOperation)] int InitialStock);
```

Os limites do `[Range]` do preço são literais `double`, porque um atributo não aceita uma conversão de uma constante `decimal`. Os valores são os mesmos de `ProductConstraints.MinPrice` e `ProductConstraints.MaxPrice`.

`Product/Product.Application/Products/DTOs/UpdateProductDTO.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Product.Domain.Constants;

namespace Product.Application.Products.DTOs;

public sealed record UpdateProductDTO(
    [Required, StringLength(ProductConstraints.NameMaxLength)] string Name,
    [StringLength(ProductConstraints.DescriptionMaxLength)] string? Description,
    [Required, StringLength(ProductConstraints.CategoryMaxLength)] string Category,
    [Range(0.01, 99_999_999.99)] decimal Price);
```

`Product/Product.Application/Products/DTOs/StockDTO.cs`:
```csharp
namespace Product.Application.Products.DTOs;

public sealed record StockDTO(int ProductId, int Quantity);
```

- [ ] **Step 7: Criar as regras partilhadas, em `Product/Product.Application/Products/Validation/ProductRules.cs`**

```csharp
using FluentValidation;
using Product.Domain.Constants;

namespace Product.Application.Products.Validation;

public static class ProductRules
{
    public static IRuleBuilderOptions<T, int> ValidProductId<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(ProductConstraints.MinId, ProductConstraints.MaxId);

    public static IRuleBuilderOptions<T, string> ValidProductName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(ProductConstraints.NameMaxLength);

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(ProductConstraints.DescriptionMaxLength);

    public static IRuleBuilderOptions<T, string> ValidCategory<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(ProductConstraints.CategoryMaxLength);

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(ProductConstraints.MinPrice)
            .LessThanOrEqualTo(ProductConstraints.MaxPrice)
            .PrecisionScale(ProductConstraints.PricePrecision, ProductConstraints.PriceScale, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, int> ValidInitialStock<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(0, ProductConstraints.MaxQuantityPerOperation);

    public static IRuleBuilderOptions<T, int> ValidStockQuantity<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, ProductConstraints.MaxQuantityPerOperation);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, ProductConstraints.MaxPageSize);

    public static IRuleBuilderOptions<T, int?> ValidCursor<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(ProductConstraints.MinId, ProductConstraints.MaxId);
}
```

- [ ] **Step 8: Criar os commands e os seus validadores**

`Products/Commands/CreateProduct/CreateProductCommand.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductDTO Product, string? UserName) : IRequest<Result<ProductDTO>>;
```

`Products/Commands/CreateProduct/CreateProductCommandValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Product).NotNull();

        When(x => x.Product is not null, () =>
        {
            RuleFor(x => x.Product.Name).ValidProductName();
            RuleFor(x => x.Product.Description).ValidDescription();
            RuleFor(x => x.Product.Category).ValidCategory();
            RuleFor(x => x.Product.Price).ValidPrice();
            RuleFor(x => x.Product.InitialStock).ValidInitialStock();
        });
    }
}
```

`Products/Commands/UpdateProduct/UpdateProductCommand.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(int Id, UpdateProductDTO Product) : IRequest<Result<ProductDTO>>;
```

`Products/Commands/UpdateProduct/UpdateProductCommandValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Product).NotNull();

        When(x => x.Product is not null, () =>
        {
            RuleFor(x => x.Product.Name).ValidProductName();
            RuleFor(x => x.Product.Description).ValidDescription();
            RuleFor(x => x.Product.Category).ValidCategory();
            RuleFor(x => x.Product.Price).ValidPrice();
        });
    }
}
```

`Products/Commands/DeleteProduct/DeleteProductCommand.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(int Id, string? UserName) : IRequest<Result>;
```

`Products/Commands/DeleteProduct/DeleteProductCommandValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator() => RuleFor(x => x.Id).ValidProductId();
}
```

`Products/Commands/AddStockToProduct/AddStockToProductCommand.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed record AddStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>;
```

`Products/Commands/AddStockToProduct/AddStockToProductCommandValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed class AddStockToProductCommandValidator : AbstractValidator<AddStockToProductCommand>
{
    public AddStockToProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Quantity).ValidStockQuantity();
    }
}
```

`Products/Commands/DecreaseStockToProduct/DecreaseStockToProductCommand.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed record DecreaseStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>;
```

`Products/Commands/DecreaseStockToProduct/DecreaseStockToProductCommandValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed class DecreaseStockToProductCommandValidator : AbstractValidator<DecreaseStockToProductCommand>
{
    public DecreaseStockToProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Quantity).ValidStockQuantity();
    }
}
```

- [ ] **Step 9: Criar as queries e os seus validadores**

`Products/Queries/GetProducts/GetProductsQuery.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int? LastItemId, int PageSize = ProductConstraints.DefaultPageSize)
    : IRequest<Result<PagedResultDTO<ProductDTO>>>;
```

`Products/Queries/GetProducts/GetProductsQueryValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
```

`Products/Queries/GetProductById/GetProductByIdQuery.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<Result<ProductDTO>>;
```

`Products/Queries/GetProductById/GetProductByIdQueryValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator() => RuleFor(x => x.Id).ValidProductId();
}
```

`Products/Queries/SearchProductsByName/SearchProductsByNameQuery.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.SearchProductsByName;

public sealed record SearchProductsByNameQuery(string Name, int? LastItemId, int PageSize = ProductConstraints.DefaultPageSize)
    : IRequest<Result<PagedResultDTO<ProductDTO>>>;
```

`Products/Queries/SearchProductsByName/SearchProductsByNameQueryValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.SearchProductsByName;

public sealed class SearchProductsByNameQueryValidator : AbstractValidator<SearchProductsByNameQuery>
{
    public SearchProductsByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(name => name.Trim().Length is >= ProductConstraints.NameSearchMinLength and <= ProductConstraints.NameMaxLength)
            .WithMessage($"'Name' must have between {ProductConstraints.NameSearchMinLength} and {ProductConstraints.NameMaxLength} characters.");
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
```

`Products/Queries/SearchProductsByStockRange/SearchProductsByStockRangeQuery.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed record SearchProductsByStockRangeQuery(int Min, int Max, int? LastItemId, int PageSize = ProductConstraints.DefaultPageSize)
    : IRequest<Result<PagedResultDTO<ProductDTO>>>;
```

`Products/Queries/SearchProductsByStockRange/SearchProductsByStockRangeQueryValidator.cs`:
```csharp
using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed class SearchProductsByStockRangeQueryValidator : AbstractValidator<SearchProductsByStockRangeQuery>
{
    public SearchProductsByStockRangeQueryValidator()
    {
        RuleFor(x => x.Min).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Max).GreaterThanOrEqualTo(x => x.Min);
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
```

- [ ] **Step 10: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Validators"`
Expected: PASS, cerca de 60 casos (contando os de `Theory`), com 0 falhas.

- [ ] **Step 11: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 4: Pipeline do MediatR, logging, mapeamento Mapster e registo da Application

Entregável:
- o `ValidationBehavior` bloqueia pedidos inválidos;
- o `LoggingBehavior` regista e devolve o resultado sem o alterar;
- o mapeamento `ProductEntity → ProductDTO` inclui o stock;
- o `AddApplication()` regista tudo.

**Files:**
- Create: `Product/Product.Application/Common/Behaviors/ValidationBehavior.cs`, `LoggingBehavior.cs`
- Create: `Product/Product.Application/Common/Logging/LogMessages.cs`
- Create: `Product/Product.Application/Products/Mapping/ProductMappingConfig.cs`
- Create: `Product/Product.Application/DependencyInjection.cs`
- Create: `UnitTests/Product.Api.UnitTest/TestData/TestProducts.cs`, `TestMapping.cs`
- Test: `UnitTests/Product.Api.UnitTest/Behaviors/ValidationBehaviorTests.cs`, `LoggingBehaviorTests.cs`
- Test: `UnitTests/Product.Api.UnitTest/Application/ProductMappingConfigTests.cs`, `ApplicationDependencyInjectionTests.cs`

**Interfaces:**
- Consumes: os pedidos e validadores (Task 3) e os tipos Result (Task 2).
- Produces:
  - Behaviors:
    - `ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>>)`, com `where TResponse : IResultBase<TResponse>`;
    - `LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>>)`.
  - `internal static partial class LogMessages`, com as extensões: `HandlingRequest`, `HandledRequest`, `RequestFailed`, `ProductCreated`, `ProductUpdated`, `ProductDeleted`, `ProductConcurrencyConflict`, `StockIncreased`, `StockDecreased`, `InsufficientStock` e `StockLimitExceeded`.
  - `ProductMappingConfig : IRegister`.
  - `DependencyInjection.AddApplication(this IServiceCollection)`, que regista o MediatR, os validadores, um `TypeAdapterConfig` singleton, o `IMapper` singleton e o `TimeProvider.System`.
  - Ajudas de teste:
    - `TestProducts.Now` (2026-11-09T10:00Z);
    - `TestProducts.Hammer(int stock = 40)`;
    - `TestProducts.HammerDTO`;
    - `TestMapping.CreateMapper()`.

- [ ] **Step 1: Criar as ajudas de teste**

`UnitTests/Product.Api.UnitTest/TestData/TestProducts.cs`:
```csharp
using Product.Application.Products.DTOs;
using Product.Domain.Entities;

namespace Product.Api.UnitTest.TestData;

internal static class TestProducts
{
    public static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ProductDTO HammerDTO => new(100001, "Martelo de Carpinteiro", "Martelo com cabo de madeira.", "Ferramentas Manuais", 12.90m, 40);

    public static ProductEntity Hammer(int stock = 40) => new()
    {
        Id = 100001,
        Name = "Martelo de Carpinteiro",
        Description = "Martelo com cabo de madeira.",
        Category = "Ferramentas Manuais",
        Price = 12.90m,
        CreatedAt = SeedDate,
        Stock = new Stock { ProductId = 100001, Quantity = stock, UpdatedAt = SeedDate }
    };
}
```

`UnitTests/Product.Api.UnitTest/TestData/TestMapping.cs`:
```csharp
using Mapster;
using MapsterMapper;
using Product.Application.Products.Mapping;

namespace Product.Api.UnitTest.TestData;

internal static class TestMapping
{
    public static IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ProductMappingConfig).Assembly);
        return new Mapper(config);
    }
}
```

- [ ] **Step 2: Escrever `UnitTests/Product.Api.UnitTest/Behaviors/ValidationBehaviorTests.cs`**

```csharp
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Behaviors;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(1), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([new GetProductByIdQueryValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(100001), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.True(nextCalled);
        Assert.Equal(TestProducts.HammerDTO, result.Value);
    }

    [Fact]
    public async Task InvalidRequest_ReturnsValidationFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([new GetProductByIdQueryValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(1), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.True(result.Error.ValidationErrors.ContainsKey("Id"));
    }
}
```

- [ ] **Step 3: Escrever `UnitTests/Product.Api.UnitTest/Behaviors/LoggingBehaviorTests.cs`**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Behaviors;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Behaviors;

public sealed class LoggingBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static LoggingBehavior<GetProductByIdQuery, Result<ProductDTO>> CreateBehavior() =>
        new(NullLogger<LoggingBehavior<GetProductByIdQuery, Result<ProductDTO>>>.Instance);

    [Fact]
    public async Task SuccessfulResponse_IsReturnedUnchanged()
    {
        Result<ProductDTO> expected = TestProducts.HammerDTO;

        var result = await CreateBehavior().Handle(new GetProductByIdQuery(100001), _ => Task.FromResult(expected), Ct);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task FailedResponse_IsReturnedUnchanged()
    {
        Result<ProductDTO> expected = new Error("Product.NotFound", "Not found.", ErrorType.NotFound);

        var result = await CreateBehavior().Handle(new GetProductByIdQuery(100001), _ => Task.FromResult(expected), Ct);

        Assert.Same(expected, result);
    }
}
```

- [ ] **Step 4: Escrever `UnitTests/Product.Api.UnitTest/Application/ProductMappingConfigTests.cs`**

```csharp
using Product.Api.UnitTest.TestData;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Application;

public sealed class ProductMappingConfigTests
{
    [Fact]
    public void MapsEntityToDTO_IncludingStockQuantity()
    {
        var dto = TestMapping.CreateMapper().Map<ProductDTO>(TestProducts.Hammer(stock: 40));

        Assert.Equal(TestProducts.HammerDTO, dto);
    }

    [Fact]
    public void EntityWithoutStock_MapsZeroQuantity()
    {
        var product = TestProducts.Hammer();
        product.Stock = null;

        var dto = TestMapping.CreateMapper().Map<ProductDTO>(product);

        Assert.Equal(0, dto.StockQuantity);
    }
}
```

- [ ] **Step 5: Escrever `UnitTests/Product.Api.UnitTest/Application/ApplicationDependencyInjectionTests.cs`**

```csharp
using FluentValidation;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Product.Application;
using Product.Application.Common.Results;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Application;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatorValidatorsMapperTimeProviderAndBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        Assert.NotNull(scopedProvider.GetService<ISender>());
        Assert.NotNull(scopedProvider.GetService<IMapper>());
        Assert.NotNull(scopedProvider.GetService<TimeProvider>());
        Assert.NotNull(scopedProvider.GetService<IValidator<CreateProductCommand>>());
        Assert.Equal(2, scopedProvider.GetServices<IPipelineBehavior<GetProductByIdQuery, Result<ProductDTO>>>().Count());
    }
}
```

- [ ] **Step 6: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Behaviors|FullyQualifiedName~Application"`
Expected: FAIL na compilação (`ValidationBehavior`, `LoggingBehavior`, `ProductMappingConfig` e `AddApplication` não existem).

- [ ] **Step 7: Criar `Product/Product.Application/Common/Logging/LogMessages.cs`**

```csharp
using Microsoft.Extensions.Logging;

namespace Product.Application.Common.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void HandlingRequest(this ILogger logger, string requestName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds} ms")]
    public static partial void HandledRequest(this ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "{RequestName} failed with {ErrorCode}: {ErrorMessage}")]
    public static partial void RequestFailed(this ILogger logger, string requestName, string errorCode, string errorMessage);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Product {ProductId} created with initial stock {Quantity}")]
    public static partial void ProductCreated(this ILogger logger, int productId, int quantity);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Product {ProductId} updated")]
    public static partial void ProductUpdated(this ILogger logger, int productId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Product {ProductId} soft deleted by {UserName}")]
    public static partial void ProductDeleted(this ILogger logger, int productId, string? userName);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Concurrency conflict while changing product {ProductId}")]
    public static partial void ProductConcurrencyConflict(this ILogger logger, int productId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Stock of product {ProductId} increased by {Quantity} to {QuantityAfter}")]
    public static partial void StockIncreased(this ILogger logger, int productId, int quantity, int quantityAfter);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Stock of product {ProductId} decreased by {Quantity} to {QuantityAfter}")]
    public static partial void StockDecreased(this ILogger logger, int productId, int quantity, int quantityAfter);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Warning, Message = "Insufficient stock for product {ProductId}: requested {Requested}, available {Available}")]
    public static partial void InsufficientStock(this ILogger logger, int productId, int requested, int available);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Warning, Message = "Stock limit exceeded for product {ProductId} when adding {Quantity}")]
    public static partial void StockLimitExceeded(this ILogger logger, int productId, int quantity);
}
```

- [ ] **Step 8: Criar `Product/Product.Application/Common/Behaviors/ValidationBehavior.cs`**

```csharp
using FluentValidation;
using MediatR;
using Product.Application.Common.Results;

namespace Product.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IReadOnlyCollection<IValidator<TRequest>> ?? validators.ToList();
        if (validatorList.Count == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(result => result.Errors).ToList();
        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var errors = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return TResponse.Failure(Error.Validation(errors));
    }
}
```

- [ ] **Step 9: Criar `Product/Product.Application/Common/Behaviors/LoggingBehavior.cs`**

```csharp
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;

namespace Product.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.HandlingRequest(requestName);
        var startTimestamp = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        logger.HandledRequest(requestName, Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
        if (response.Error is { } error)
        {
            logger.RequestFailed(requestName, error.Code, error.Message);
        }

        return response;
    }
}
```

- [ ] **Step 10: Criar `Product/Product.Application/Products/Mapping/ProductMappingConfig.cs`**

```csharp
using Mapster;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Mapping;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<ProductEntity, ProductDTO>()
            .Map(dest => dest.StockQuantity, src => src.Stock != null ? src.Stock.Quantity : 0);
}
```

- [ ] **Step 11: Criar `Product/Product.Application/DependencyInjection.cs`**

```csharp
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Product.Application.Common.Behaviors;

namespace Product.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var mappingConfig = new TypeAdapterConfig();
        mappingConfig.Scan(assembly);
        services.AddSingleton(mappingConfig);
        services.AddSingleton<IMapper>(new Mapper(mappingConfig));
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
```

- [ ] **Step 12: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Behaviors|FullyQualifiedName~Application"`
Expected: PASS, 8 testes.

- [ ] **Step 13: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 5: Queries — abstração de leitura, erros de produto e os 4 handlers

Entregável: `GetProducts`, `GetProductById`, `SearchProductsByName` e `SearchProductsByStockRange` delegam no `IProductQueries` com os filtros certos, e o `GetProductById` devolve `NotFound` quando o produto não existe.

**Files:**
- Create: `Product/Product.Application/Products/Abstractions/ProductFilter.cs`, `IProductQueries.cs`
- Create: `Product/Product.Application/Products/ProductErrors.cs`
- Create: `Products/Queries/GetProducts/GetProductsQueryHandler.cs`, `Products/Queries/GetProductById/GetProductByIdQueryHandler.cs`, `Products/Queries/SearchProductsByName/SearchProductsByNameQueryHandler.cs`, `Products/Queries/SearchProductsByStockRange/SearchProductsByStockRangeQueryHandler.cs`
- Test: `UnitTests/Product.Api.UnitTest/Handlers/Queries/ProductQueryHandlersTests.cs`

**Interfaces:**
- Produces:
  - `sealed record ProductFilter(string? NameContains = null, int? MinStock = null, int? MaxStock = null)`, com `static ProductFilter None`;
  - `IProductQueries`, com:
    - `Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)`;
    - `Task<PagedResultDTO<ProductDTO>> GetPageAsync(ProductFilter filter, int? lastItemId, int pageSize, CancellationToken cancellationToken)`.
  - `static class ProductErrors`, com:
    - `NotFound(int id)` → `Product.NotFound` / NotFound;
    - `InsufficientStock(int id, int available, int requested)` → `Product.InsufficientStock` / BusinessRule;
    - `StockLimitExceeded(int id)` → `Product.StockLimitExceeded` / BusinessRule;
    - `ConcurrencyConflict(int id)` → `Product.ConcurrencyConflict` / Conflict.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Handlers/Queries/ProductQueryHandlersTests.cs`**

```csharp
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Handlers.Queries;

public sealed class ProductQueryHandlersTests
{
    private static readonly PagedResultDTO<ProductDTO> Page = new([TestProducts.HammerDTO], 100001, true);

    private readonly Mock<IProductQueries> _queries = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetProducts_UsesNoFilterAndForwardsPaging()
    {
        _queries.Setup(q => q.GetPageAsync(ProductFilter.None, 100005, 5, It.IsAny<CancellationToken>())).ReturnsAsync(Page);

        var result = await new GetProductsQueryHandler(_queries.Object).Handle(new GetProductsQuery(100005, 5), Ct);

        Assert.True(result.IsSuccess);
        Assert.Same(Page, result.Value);
    }

    [Fact]
    public async Task GetProductById_Found_ReturnsProduct()
    {
        _queries.Setup(q => q.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.HammerDTO);

        var result = await new GetProductByIdQueryHandler(_queries.Object).Handle(new GetProductByIdQuery(100001), Ct);

        Assert.Equal(TestProducts.HammerDTO, result.Value);
    }

    [Fact]
    public async Task GetProductById_Missing_ReturnsNotFound()
    {
        _queries.Setup(q => q.GetByIdAsync(100999, It.IsAny<CancellationToken>())).ReturnsAsync((ProductDTO?)null);

        var result = await new GetProductByIdQueryHandler(_queries.Object).Handle(new GetProductByIdQuery(100999), Ct);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task SearchProductsByName_TrimsNameIntoFilter()
    {
        _queries.Setup(q => q.GetPageAsync(new ProductFilter("mart", null, null), null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page);

        var result = await new SearchProductsByNameQueryHandler(_queries.Object)
            .Handle(new SearchProductsByNameQuery("  mart ", null, 5), Ct);

        Assert.Same(Page, result.Value);
    }

    [Fact]
    public async Task SearchProductsByStockRange_UsesMinAndMaxInFilter()
    {
        _queries.Setup(q => q.GetPageAsync(new ProductFilter(null, 10, 30), 100002, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page);

        var result = await new SearchProductsByStockRangeQueryHandler(_queries.Object)
            .Handle(new SearchProductsByStockRangeQuery(10, 30, 100002, 5), Ct);

        Assert.Same(Page, result.Value);
    }
}
```

- [ ] **Step 2: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~ProductQueryHandlersTests"`
Expected: FAIL na compilação (`IProductQueries`, `ProductFilter` e os handlers não existem).

- [ ] **Step 3: Criar as abstrações e os erros**

`Product/Product.Application/Products/Abstractions/ProductFilter.cs`:
```csharp
namespace Product.Application.Products.Abstractions;

public sealed record ProductFilter(string? NameContains = null, int? MinStock = null, int? MaxStock = null)
{
    public static ProductFilter None { get; } = new();
}
```

`Product/Product.Application/Products/Abstractions/IProductQueries.cs`:
```csharp
using Product.Application.Common.DTOs;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Abstractions;

public interface IProductQueries
{
    Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PagedResultDTO<ProductDTO>> GetPageAsync(
        ProductFilter filter,
        int? lastItemId,
        int pageSize,
        CancellationToken cancellationToken);
}
```

`Product/Product.Application/Products/ProductErrors.cs`:
```csharp
using Product.Application.Common.Results;

namespace Product.Application.Products;

public static class ProductErrors
{
    public static Error NotFound(int id) =>
        new("Product.NotFound", $"Product with id {id} was not found.", ErrorType.NotFound);

    public static Error InsufficientStock(int id, int available, int requested) =>
        new("Product.InsufficientStock",
            $"Product {id} has {available} units in stock and {requested} were requested.",
            ErrorType.BusinessRule);

    public static Error StockLimitExceeded(int id) =>
        new("Product.StockLimitExceeded",
            $"Adding this quantity would exceed the maximum stock allowed for product {id}.",
            ErrorType.BusinessRule);

    public static Error ConcurrencyConflict(int id) =>
        new("Product.ConcurrencyConflict",
            $"Product {id} was modified by another request. Reload it and try again.",
            ErrorType.Conflict);
}
```

- [ ] **Step 4: Criar os 4 handlers**

`Products/Queries/GetProducts/GetProductsQueryHandler.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductsQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(ProductFilter.None, request.LastItemId, request.PageSize, cancellationToken);
}
```

`Products/Queries/GetProductById/GetProductByIdQueryHandler.cs`:
```csharp
using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await queries.GetByIdAsync(request.Id, cancellationToken);
        return product is null ? ProductErrors.NotFound(request.Id) : product;
    }
}
```

`Products/Queries/SearchProductsByName/SearchProductsByNameQueryHandler.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.SearchProductsByName;

public sealed class SearchProductsByNameQueryHandler(IProductQueries queries)
    : IRequestHandler<SearchProductsByNameQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(SearchProductsByNameQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(new ProductFilter(NameContains: request.Name.Trim()), request.LastItemId, request.PageSize, cancellationToken);
}
```

`Products/Queries/SearchProductsByStockRange/SearchProductsByStockRangeQueryHandler.cs`:
```csharp
using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed class SearchProductsByStockRangeQueryHandler(IProductQueries queries)
    : IRequestHandler<SearchProductsByStockRangeQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(SearchProductsByStockRangeQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(
            new ProductFilter(MinStock: request.Min, MaxStock: request.Max),
            request.LastItemId,
            request.PageSize,
            cancellationToken);
}
```

- [ ] **Step 5: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~ProductQueryHandlersTests"`
Expected: PASS, 5 testes.

- [ ] **Step 6: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 6: Commands Create, Update e Delete (soft delete)

Entregável:
- o Create cria o produto, o stock e o movimento `Initial`;
- o Update altera os dados e devolve `Conflict` quando há concorrência;
- o Delete marca o produto como apagado (`IsDeleted`, `DeletedAt`, `DeletedBy`);
- todos devolvem `NotFound` quando o produto não existe.

**Files:**
- Create: `Product/Product.Application/Common/Abstractions/IUnitOfWork.cs`
- Create: `Product/Product.Application/Common/Exceptions/ConcurrencyConflictException.cs`
- Create: `Product/Product.Application/Products/Abstractions/IProductRepository.cs`
- Create: `Products/Commands/CreateProduct/CreateProductCommandHandler.cs`, `Products/Commands/UpdateProduct/UpdateProductCommandHandler.cs`, `Products/Commands/DeleteProduct/DeleteProductCommandHandler.cs`
- Test: `UnitTests/Product.Api.UnitTest/Handlers/Commands/CreateProductCommandHandlerTests.cs`, `UpdateProductCommandHandlerTests.cs`, `DeleteProductCommandHandlerTests.cs`

**Interfaces:**
- Produces:
  - `IUnitOfWork`, com:
    - `Task<int> SaveChangesAsync(CancellationToken cancellationToken)`, que lança `ConcurrencyConflictException` quando há conflito;
    - `Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken) where TResult : IResultBase<TResult>`, que faz commit só se `IsSuccess`.
  - `ConcurrencyConflictException(string message, Exception innerException)`.
  - `IProductRepository`, com:
    - `Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)` (com tracking e `Stock` incluído);
    - `void Add(ProductEntity product)`;
    - `Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)`;
    - `Task<int> TryAddStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken)`;
    - `Task<int> TryDecreaseStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken)`;
    - `Task<int> GetStockQuantityAsync(int id, CancellationToken cancellationToken)`;
    - `void AddMovement(StockMovement movement)`.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Handlers/Commands/CreateProductCommandHandlerTests.cs`**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;
using Product.Domain.Enums;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class CreateProductCommandHandlerTests
{
    private const int GeneratedId = 100011;

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProductCommandHandler _handler;
    private ProductEntity? _addedProduct;

    public CreateProductCommandHandlerTests()
    {
        _repository.Setup(r => r.Add(It.IsAny<ProductEntity>())).Callback<ProductEntity>(product => _addedProduct = product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _addedProduct!.Id = GeneratedId)
            .ReturnsAsync(3);

        _handler = new CreateProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            TestMapping.CreateMapper(),
            new FakeTimeProvider(TestProducts.Now),
            NullLogger<CreateProductCommandHandler>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithInitialStock_CreatesProductStockAndInitialMovement()
    {
        var command = new CreateProductCommand(new CreateProductDTO("Grampos", "Caixa de 100", "Fixação", 3.50m, 10), "Admin");

        var result = await _handler.Handle(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ProductDTO(GeneratedId, "Grampos", "Caixa de 100", "Fixação", 3.50m, 10), result.Value);
        Assert.NotNull(_addedProduct);
        Assert.Equal(TestProducts.Now, _addedProduct!.CreatedAt);
        Assert.Equal(10, _addedProduct.Stock!.Quantity);
        Assert.Equal(TestProducts.Now, _addedProduct.Stock.UpdatedAt);

        var movement = Assert.Single(_addedProduct.StockMovements);
        Assert.Equal(StockMovementType.Initial, movement.Type);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal(10, movement.QuantityAfter);
        Assert.Equal("Admin", movement.CreatedBy);
        Assert.Equal(TestProducts.Now, movement.CreatedAt);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithZeroInitialStock_CreatesStockWithoutMovement()
    {
        var command = new CreateProductCommand(new CreateProductDTO("Grampos", null, "Fixação", 3.50m, 0), "Admin");

        var result = await _handler.Handle(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _addedProduct!.Stock!.Quantity);
        Assert.Empty(_addedProduct.StockMovements);
    }

    [Fact]
    public async Task TrimsTextFieldsAndTurnsBlankDescriptionIntoNull()
    {
        var command = new CreateProductCommand(new CreateProductDTO("  Grampos  ", "   ", " Fixação ", 3.50m, 1), "Admin");

        await _handler.Handle(command, Ct);

        Assert.Equal("Grampos", _addedProduct!.Name);
        Assert.Null(_addedProduct.Description);
        Assert.Equal("Fixação", _addedProduct.Category);
    }
}
```

- [ ] **Step 2: Escrever `UnitTests/Product.Api.UnitTest/Handlers/Commands/UpdateProductCommandHandlerTests.cs`**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class UpdateProductCommandHandlerTests
{
    private static readonly UpdateProductDTO Changes = new(" Martelo Pro ", "Cabo de fibra.", "Ferramentas Manuais", 15.50m);

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests() =>
        _handler = new UpdateProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            TestMapping.CreateMapper(),
            new FakeTimeProvider(TestProducts.Now),
            NullLogger<UpdateProductCommandHandler>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnknownProduct_ReturnsNotFoundWithoutSaving()
    {
        _repository.Setup(r => r.GetByIdAsync(100999, It.IsAny<CancellationToken>())).ReturnsAsync((ProductEntity?)null);

        var result = await _handler.Handle(new UpdateProductCommand(100999, Changes), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExistingProduct_UpdatesFieldsAndReturnsDTO()
    {
        var product = TestProducts.Hammer(stock: 40);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new UpdateProductCommand(100001, Changes), Ct);

        Assert.Equal(new ProductDTO(100001, "Martelo Pro", "Cabo de fibra.", "Ferramentas Manuais", 15.50m, 40), result.Value);
        Assert.Equal(TestProducts.Now, product.UpdatedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentChange_ReturnsConflict()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.Hammer());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new InvalidOperationException()));

        var result = await _handler.Handle(new UpdateProductCommand(100001, Changes), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("Product.ConcurrencyConflict", result.Error.Code);
    }
}
```

- [ ] **Step 3: Escrever `UnitTests/Product.Api.UnitTest/Handlers/Commands/DeleteProductCommandHandlerTests.cs`**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.DeleteProduct;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests() =>
        _handler = new DeleteProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            new FakeTimeProvider(TestProducts.Now),
            NullLogger<DeleteProductCommandHandler>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExistingProduct_IsSoftDeleted()
    {
        var product = TestProducts.Hammer();
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.True(result.IsSuccess);
        Assert.True(product.IsDeleted);
        Assert.Equal(TestProducts.Now, product.DeletedAt);
        Assert.Equal("Admin", product.DeletedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingOrAlreadyDeletedProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync((ProductEntity?)null);

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentChange_ReturnsConflict()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.Hammer());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new InvalidOperationException()));

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
```

- [ ] **Step 4: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Handlers.Commands"`
Expected: FAIL na compilação (`IProductRepository`, `IUnitOfWork`, `ConcurrencyConflictException` e os handlers não existem).

- [ ] **Step 5: Criar as abstrações de escrita**

`Product/Product.Application/Common/Abstractions/IUnitOfWork.cs`:
```csharp
using Product.Application.Common.Results;

namespace Product.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : IResultBase<TResult>;
}
```

`Product/Product.Application/Common/Exceptions/ConcurrencyConflictException.cs`:
```csharp
namespace Product.Application.Common.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
```

`Product/Product.Application/Products/Abstractions/IProductRepository.cs`:
```csharp
using Product.Domain.Entities;

namespace Product.Application.Products.Abstractions;

public interface IProductRepository
{
    Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);

    void Add(ProductEntity product);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<int> TryAddStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> TryDecreaseStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> GetStockQuantityAsync(int id, CancellationToken cancellationToken);

    void AddMovement(StockMovement movement);
}
```

- [ ] **Step 6: Criar `Products/Commands/CreateProduct/CreateProductCommandHandler.cs`**

```csharp
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<CreateProductCommandHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var data = request.Product;

        var product = new ProductEntity
        {
            Name = data.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            Category = data.Category.Trim(),
            Price = data.Price,
            CreatedAt = now,
            Stock = new Stock { Quantity = data.InitialStock, UpdatedAt = now }
        };

        if (data.InitialStock > 0)
        {
            product.StockMovements.Add(new StockMovement
            {
                Type = StockMovementType.Initial,
                Quantity = data.InitialStock,
                QuantityAfter = data.InitialStock,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
        }

        repository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.ProductCreated(product.Id, data.InitialStock);
        return mapper.Map<ProductDTO>(product);
    }
}
```

- [ ] **Step 7: Criar `Products/Commands/UpdateProduct/UpdateProductCommandHandler.cs`**

```csharp
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<UpdateProductCommandHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        var data = request.Product;
        product.Name = data.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        product.Category = data.Category.Trim();
        product.Price = data.Price;
        product.UpdatedAt = timeProvider.GetUtcNow();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.ProductConcurrencyConflict(request.Id);
            return ProductErrors.ConcurrencyConflict(request.Id);
        }

        logger.ProductUpdated(request.Id);
        return mapper.Map<ProductDTO>(product);
    }
}
```

- [ ] **Step 8: Criar `Products/Commands/DeleteProduct/DeleteProductCommandHandler.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DeleteProductCommandHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        product.IsDeleted = true;
        product.DeletedAt = timeProvider.GetUtcNow();
        product.DeletedBy = request.UserName;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.ProductConcurrencyConflict(request.Id);
            return ProductErrors.ConcurrencyConflict(request.Id);
        }

        logger.ProductDeleted(request.Id, request.UserName);
        return Result.Success();
    }
}
```

- [ ] **Step 9: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~Handlers.Commands"`
Expected: PASS, 9 testes.

- [ ] **Step 10: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 7: Commands de stock (AddStockToProduct e DecreaseStockToProduct)

Entregável: as alterações de stock correm numa transação do UnitOfWork, usam o `UPDATE` condicional do repositório, registam o movimento e devolvem `NotFound`, `InsufficientStock` ou `StockLimitExceeded` conforme o caso.

**Files:**
- Create: `Products/Commands/AddStockToProduct/AddStockToProductCommandHandler.cs`
- Create: `Products/Commands/DecreaseStockToProduct/DecreaseStockToProductCommandHandler.cs`
- Test: `UnitTests/Product.Api.UnitTest/Handlers/Commands/StockCommandHandlersTests.cs`

**Interfaces:**
- Consumes: `IProductRepository`, `IUnitOfWork` (Task 6), `ProductErrors` (Task 5) e `LogMessages` (Task 4).
- Produces: `AddStockToProductCommandHandler` e `DecreaseStockToProductCommandHandler`, ambos com o construtor `(IProductRepository, IUnitOfWork, TimeProvider, ILogger<Self>)`.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Handlers/Commands/StockCommandHandlersTests.cs`**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class StockCommandHandlersTests
{
    private const int ProductId = 100001;

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new(TestProducts.Now);
    private StockMovement? _addedMovement;

    public StockCommandHandlersTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<StockDTO>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<StockDTO>>> operation, CancellationToken token) => operation(token));
        _repository.Setup(r => r.AddMovement(It.IsAny<StockMovement>())).Callback<StockMovement>(m => _addedMovement = m);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AddStockToProductCommandHandler AddHandler() =>
        new(_repository.Object, _unitOfWork.Object, _timeProvider, NullLogger<AddStockToProductCommandHandler>.Instance);

    private DecreaseStockToProductCommandHandler DecreaseHandler() =>
        new(_repository.Object, _unitOfWork.Object, _timeProvider, NullLogger<DecreaseStockToProductCommandHandler>.Instance);

    [Fact]
    public async Task AddStock_UpdatesStockRecordsMovementAndRunsInTransaction()
    {
        _repository.Setup(r => r.TryAddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(50);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal(new StockDTO(ProductId, 50), result.Value);
        Assert.NotNull(_addedMovement);
        Assert.Equal(StockMovementType.Increase, _addedMovement!.Type);
        Assert.Equal(10, _addedMovement.Quantity);
        Assert.Equal(50, _addedMovement.QuantityAfter);
        Assert.Equal("Manager", _addedMovement.CreatedBy);
        Assert.Equal(TestProducts.Now, _addedMovement.CreatedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(
            It.IsAny<Func<CancellationToken, Task<Result<StockDTO>>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStock_UnknownProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.TryAddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal("Product.NotFound", result.Error!.Code);
        Assert.Null(_addedMovement);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddStock_ExistingProductNotUpdated_ReturnsStockLimitExceeded()
    {
        _repository.Setup(r => r.TryAddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
        Assert.Equal("Product.StockLimitExceeded", result.Error.Code);
    }

    [Fact]
    public async Task DecreaseStock_UpdatesStockAndRecordsMovement()
    {
        _repository.Setup(r => r.TryDecreaseStockAsync(ProductId, 5, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(35);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 5, "Manager"), Ct);

        Assert.Equal(new StockDTO(ProductId, 35), result.Value);
        Assert.Equal(StockMovementType.Decrease, _addedMovement!.Type);
        Assert.Equal(5, _addedMovement.Quantity);
        Assert.Equal(35, _addedMovement.QuantityAfter);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecreaseStock_NotEnoughStock_ReturnsInsufficientStock()
    {
        _repository.Setup(r => r.TryDecreaseStockAsync(ProductId, 50, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 50, "Manager"), Ct);

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
        Assert.Equal("Product.InsufficientStock", result.Error.Code);
        Assert.Contains("3", result.Error.Message);
        Assert.Null(_addedMovement);
    }

    [Fact]
    public async Task DecreaseStock_UnknownProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.TryDecreaseStockAsync(ProductId, 5, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 5, "Manager"), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
```

- [ ] **Step 2: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~StockCommandHandlersTests"`
Expected: FAIL na compilação (os dois handlers não existem).

- [ ] **Step 3: Criar `Products/Commands/AddStockToProduct/AddStockToProductCommandHandler.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed class AddStockToProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AddStockToProductCommandHandler> logger)
    : IRequestHandler<AddStockToProductCommand, Result<StockDTO>>
{
    public Task<Result<StockDTO>> Handle(AddStockToProductCommand request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<Result<StockDTO>>(async token =>
        {
            var now = timeProvider.GetUtcNow();
            var affectedRows = await repository.TryAddStockAsync(request.Id, request.Quantity, now, token);
            if (affectedRows == 0)
            {
                if (!await repository.ExistsAsync(request.Id, token))
                {
                    return ProductErrors.NotFound(request.Id);
                }

                logger.StockLimitExceeded(request.Id, request.Quantity);
                return ProductErrors.StockLimitExceeded(request.Id);
            }

            var quantityAfter = await repository.GetStockQuantityAsync(request.Id, token);
            repository.AddMovement(new StockMovement
            {
                ProductId = request.Id,
                Type = StockMovementType.Increase,
                Quantity = request.Quantity,
                QuantityAfter = quantityAfter,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
            await unitOfWork.SaveChangesAsync(token);

            logger.StockIncreased(request.Id, request.Quantity, quantityAfter);
            return new StockDTO(request.Id, quantityAfter);
        }, cancellationToken);
}
```

- [ ] **Step 4: Criar `Products/Commands/DecreaseStockToProduct/DecreaseStockToProductCommandHandler.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed class DecreaseStockToProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DecreaseStockToProductCommandHandler> logger)
    : IRequestHandler<DecreaseStockToProductCommand, Result<StockDTO>>
{
    public Task<Result<StockDTO>> Handle(DecreaseStockToProductCommand request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<Result<StockDTO>>(async token =>
        {
            var now = timeProvider.GetUtcNow();
            var affectedRows = await repository.TryDecreaseStockAsync(request.Id, request.Quantity, now, token);
            if (affectedRows == 0)
            {
                if (!await repository.ExistsAsync(request.Id, token))
                {
                    return ProductErrors.NotFound(request.Id);
                }

                var available = await repository.GetStockQuantityAsync(request.Id, token);
                logger.InsufficientStock(request.Id, request.Quantity, available);
                return ProductErrors.InsufficientStock(request.Id, available, request.Quantity);
            }

            var quantityAfter = await repository.GetStockQuantityAsync(request.Id, token);
            repository.AddMovement(new StockMovement
            {
                ProductId = request.Id,
                Type = StockMovementType.Decrease,
                Quantity = request.Quantity,
                QuantityAfter = quantityAfter,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
            await unitOfWork.SaveChangesAsync(token);

            logger.StockDecreased(request.Id, request.Quantity, quantityAfter);
            return new StockDTO(request.Id, quantityAfter);
        }, cancellationToken);
}
```

- [ ] **Step 5: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~StockCommandHandlersTests"`
Expected: PASS, 6 testes.

- [ ] **Step 6: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 8: Infrastructure — DbContext, configurações, seed, repositórios, UnitOfWork e migration

Entregável: a migration `InitialCreate` cria a base `StoreHouse` com a extensão `pg_trgm`, a sequence, as 3 tabelas, os índices, os `CHECK`s e o seed dos 10 produtos.

**Files:**
- Create: `Product/Product.Infrastructure/Persistence/ProductDbContext.cs`, `UnitOfWork.cs`, `LikePatternEscaper.cs`
- Create: `Product/Product.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`, `StockConfiguration.cs`, `StockMovementConfiguration.cs`
- Create: `Product/Product.Infrastructure/Persistence/Seed/ProductSeedData.cs`
- Create: `Product/Product.Infrastructure/Products/ProductRepository.cs`, `ProductQueries.cs`
- Create: `Product/Product.Infrastructure/DependencyInjection.cs`
- Modify: `Product/Product.Api/Program.cs` (substituir na totalidade; é necessário para o `dotnet ef`)
- Generated: `Product/Product.Infrastructure/Persistence/Migrations/*`
- Test: `UnitTests/Product.Api.UnitTest/Infrastructure/LikePatternEscaperTests.cs`

**Interfaces:**
- Consumes: as interfaces da Application (`IProductRepository`, `IProductQueries`, `IUnitOfWork`, `ProductFilter`, `ConcurrencyConflictException`), o `TypeAdapterConfig` registado pelo `AddApplication()` e as entidades do Domain.
- Produces:
  - `ProductDbContext`, com `Products`, `Stocks` e `StockMovements`;
  - `DependencyInjection.AddInfrastructure(this IServiceCollection, string connectionString)`;
  - `DependencyInjection.MigrateProductDatabaseAsync(this IServiceProvider, CancellationToken = default)`;
  - `LikePatternEscaper.Escape(string)` e `LikePatternEscaper.EscapeCharacter`.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Infrastructure/LikePatternEscaperTests.cs`**

```csharp
using Product.Infrastructure.Persistence;

namespace Product.Api.UnitTest.Infrastructure;

public sealed class LikePatternEscaperTests
{
    [Theory]
    [InlineData("martelo", "martelo")]
    [InlineData("50%", @"50\%")]
    [InlineData("a_b", @"a\_b")]
    [InlineData(@"c:\temp", @"c:\\temp")]
    [InlineData(@"%_\", @"\%\_\\")]
    public void Escape_EscapesLikeWildcards(string input, string expected) =>
        Assert.Equal(expected, LikePatternEscaper.Escape(input));
}
```

- [ ] **Step 2: Correr o teste e confirmar que falha**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~LikePatternEscaperTests"`
Expected: FAIL na compilação (`LikePatternEscaper` não existe).

- [ ] **Step 3: Criar `Persistence/LikePatternEscaper.cs`**

```csharp
namespace Product.Infrastructure.Persistence;

public static class LikePatternEscaper
{
    public const string EscapeCharacter = @"\";

    public static string Escape(string value) =>
        value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
```

- [ ] **Step 4: Correr o teste e confirmar que passa**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~LikePatternEscaperTests"`
Expected: PASS, 5 casos.

- [ ] **Step 5: Criar `Persistence/Seed/ProductSeedData.cs`**

```csharp
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Infrastructure.Persistence.Seed;

internal static class ProductSeedData
{
    private const string SeedUser = "seed";
    private static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly (int Id, string Name, string Description, string Category, decimal Price, int Quantity)[] Items =
    [
        (100001, "Martelo de Carpinteiro", "Martelo com cabo de madeira e cabeça de aço forjado.", "Ferramentas Manuais", 12.90m, 40),
        (100002, "Berbequim sem Fios 18V", "Berbequim com bateria de lítio e duas velocidades.", "Ferramentas Elétricas", 89.99m, 15),
        (100003, "Fita Métrica 5m", "Fita métrica com travão e caixa antichoque.", "Medição", 6.50m, 120),
        (100004, "Alicate Universal", "Alicate de 180 mm com punhos isolados.", "Ferramentas Manuais", 9.75m, 60),
        (100005, "Serrote de Costas", "Serrote para cortes de precisão em madeira.", "Ferramentas Manuais", 14.20m, 25),
        (100006, "Nível de Bolha 60cm", "Nível em alumínio com três bolhas.", "Medição", 11.40m, 30),
        (100007, "Jogo de Chaves de Fendas", "Conjunto de seis chaves planas e Phillips.", "Ferramentas Manuais", 18.90m, 45),
        (100008, "Caixa de Parafusos 200un", "Parafusos para madeira de vários tamanhos.", "Fixação", 7.30m, 200),
        (100009, "Lixa para Madeira P120", "Folha de lixa de grão médio.", "Consumíveis", 1.20m, 500),
        (100010, "Cola de Madeira 750ml", "Cola vinílica de secagem rápida.", "Consumíveis", 8.60m, 80)
    ];

    public static IEnumerable<ProductEntity> Products => Items.Select(item => new ProductEntity
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Category = item.Category,
        Price = item.Price,
        CreatedAt = SeedDate
    });

    public static IEnumerable<Stock> Stocks => Items.Select(item => new Stock
    {
        ProductId = item.Id,
        Quantity = item.Quantity,
        UpdatedAt = SeedDate
    });

    public static IEnumerable<StockMovement> Movements => Items.Select((item, index) => new StockMovement
    {
        Id = index + 1,
        ProductId = item.Id,
        Type = StockMovementType.Initial,
        Quantity = item.Quantity,
        QuantityAfter = item.Quantity,
        CreatedAt = SeedDate,
        CreatedBy = SeedUser
    });
}
```

- [ ] **Step 6: Criar `Persistence/Configurations/ProductConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Constants;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public const string IdSequenceName = "ProductIdSequence";
    private const string NotDeletedFilter = "\"IsDeleted\" = false";

    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("Product", table => table.HasCheckConstraint("CK_Product_Price", "\"Price\" > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseSequence(IdSequenceName);
        builder.Property(p => p.Name).HasMaxLength(ProductConstraints.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(ProductConstraints.DescriptionMaxLength);
        builder.Property(p => p.Category).HasMaxLength(ProductConstraints.CategoryMaxLength).IsRequired();
        builder.Property(p => p.Price).HasPrecision(ProductConstraints.PricePrecision, ProductConstraints.PriceScale);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.Property(p => p.DeletedBy).HasMaxLength(ProductConstraints.UserNameMaxLength);
        builder.Property(p => p.Version).IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Product_Name_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasFilter(NotDeletedFilter);
        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Product_Category")
            .HasFilter(NotDeletedFilter);

        builder.HasOne(p => p.Stock)
            .WithOne(s => s.Product)
            .HasForeignKey<Stock>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.StockMovements)
            .WithOne(m => m.Product)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(ProductSeedData.Products);
    }
}
```

- [ ] **Step 7: Criar `Persistence/Configurations/StockConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stock", table => table.HasCheckConstraint("CK_Stock_Quantity", "\"Quantity\" >= 0"));

        builder.HasKey(s => s.ProductId);
        builder.Property(s => s.ProductId).ValueGeneratedNever();
        builder.Property(s => s.Version).IsRowVersion();

        builder.HasQueryFilter(s => !s.Product!.IsDeleted);
        builder.HasIndex(s => s.Quantity).HasDatabaseName("IX_Stock_Quantity");

        builder.HasData(ProductSeedData.Stocks);
    }
}
```

- [ ] **Step 8: Criar `Persistence/Configurations/StockMovementConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Constants;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovement", table =>
        {
            table.HasCheckConstraint("CK_StockMovement_Type", "\"Type\" IN (1, 2, 3)");
            table.HasCheckConstraint("CK_StockMovement_Quantity", "\"Quantity\" > 0");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 11);
        builder.Property(m => m.Type).HasConversion<short>();
        builder.Property(m => m.CreatedBy).HasMaxLength(ProductConstraints.UserNameMaxLength);

        builder.HasQueryFilter(m => !m.Product!.IsDeleted);
        builder.HasIndex(m => new { m.ProductId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_StockMovement_ProductId_CreatedAt");

        builder.HasData(ProductSeedData.Movements);
    }
}
```

- [ ] **Step 9: Criar `Persistence/ProductDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Configurations;

namespace Product.Infrastructure.Persistence;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<Stock> Stocks => Set<Stock>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasSequence<int>(ProductConfiguration.IdSequenceName)
            .StartsAt(100_011)
            .IncrementsBy(1)
            .HasMin(100_000)
            .HasMax(999_999)
            .IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
    }
}
```

- [ ] **Step 10: Criar `Persistence/UnitOfWork.cs`**

O `ChangeTracker.Clear()` no início de cada tentativa garante que uma nova tentativa da execution strategy não volta a inserir movimentos que ficaram da tentativa falhada.

```csharp
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;

namespace Product.Infrastructure.Persistence;

internal sealed class UnitOfWork(ProductDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The entity was modified by another operation.", exception);
        }
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : IResultBase<TResult>
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(token);

            var result = await operation(token);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(token);
            }
            else
            {
                await transaction.RollbackAsync(token);
            }

            return result;
        }, cancellationToken);
    }
}
```

- [ ] **Step 11: Criar `Products/ProductRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Product.Application.Products.Abstractions;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Products;

internal sealed class ProductRepository(ProductDbContext context) : IProductRepository
{
    public Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(ProductEntity product) => context.Products.Add(product);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(p => p.Id == id, cancellationToken);

    public Task<int> TryAddStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var maximumCurrentQuantity = int.MaxValue - quantity;
        return context.Stocks
            .Where(s => s.ProductId == id && s.Quantity <= maximumCurrentQuantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Quantity, s => s.Quantity + quantity)
                .SetProperty(s => s.UpdatedAt, now), cancellationToken);
    }

    public Task<int> TryDecreaseStockAsync(int id, int quantity, DateTimeOffset now, CancellationToken cancellationToken) =>
        context.Stocks
            .Where(s => s.ProductId == id && s.Quantity >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Quantity, s => s.Quantity - quantity)
                .SetProperty(s => s.UpdatedAt, now), cancellationToken);

    public Task<int> GetStockQuantityAsync(int id, CancellationToken cancellationToken) =>
        context.Stocks
            .Where(s => s.ProductId == id)
            .Select(s => s.Quantity)
            .FirstAsync(cancellationToken);

    public void AddMovement(StockMovement movement) => context.StockMovements.Add(movement);
}
```

- [ ] **Step 12: Criar `Products/ProductQueries.cs`**

```csharp
using Mapster;
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.DTOs;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Products;

internal sealed class ProductQueries(ProductDbContext context, TypeAdapterConfig mappingConfig) : IProductQueries
{
    public Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectToType<ProductDTO>(mappingConfig)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResultDTO<ProductDTO>> GetPageAsync(
        ProductFilter filter,
        int? lastItemId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductEntity> query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var pattern = $"%{LikePatternEscaper.Escape(filter.NameContains)}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, LikePatternEscaper.EscapeCharacter));
        }

        if (filter.MinStock is int minStock)
        {
            query = query.Where(p => p.Stock!.Quantity >= minStock);
        }

        if (filter.MaxStock is int maxStock)
        {
            query = query.Where(p => p.Stock!.Quantity <= maxStock);
        }

        if (lastItemId is int cursor)
        {
            query = query.Where(p => p.Id > cursor);
        }

        var items = await query
            .OrderBy(p => p.Id)
            .Take(pageSize + 1)
            .ProjectToType<ProductDTO>(mappingConfig)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new PagedResultDTO<ProductDTO>(items, hasMore ? items[^1].Id : null, hasMore);
    }
}
```

- [ ] **Step 13: Criar `Product/Product.Infrastructure/DependencyInjection.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Common.Abstractions;
using Product.Application.Products.Abstractions;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Products;

namespace Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextPool<ProductDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task MigrateProductDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
```

- [ ] **Step 14: Substituir `Product/Product.Api/Program.cs` (registo das camadas, necessário para o `dotnet ef`)**

```csharp
using Product.Application;
using Product.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouse")
    ?? throw new InvalidOperationException("Connection string 'StoreHouse' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 15: Configurar a connection string em User Secrets**

Run:
```powershell
dotnet user-secrets set "ConnectionStrings:StoreHouse" "Host=localhost;Port=5432;Database=StoreHouse;Username=postgres;Password=<A_TUA_PASSWORD>;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50" --project Product/Product.Api
```
Expected: `Successfully saved ConnectionStrings:StoreHouse to the secret store.`

- [ ] **Step 16: Compilar e gerar a migration**

Run:
```powershell
dotnet build StoreHouse.slnx
dotnet ef migrations add InitialCreate --project Product/Product.Infrastructure --startup-project Product/Product.Api --output-dir Persistence/Migrations
```
Expected: `Build succeeded` e `Done.`

- [ ] **Step 17: Inspecionar a migration**

Abrir `Product/Product.Infrastructure/Persistence/Migrations/*_InitialCreate.cs` e confirmar que existem:
1. `.Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")`.
2. `migrationBuilder.CreateSequence<int>(name: "ProductIdSequence", startValue: 100011L, minValue: 100000L, maxValue: 999999L)`.
3. A tabela `Product`:
   - a coluna `Id` com `defaultValueSql: "nextval('\"ProductIdSequence\"')"`, sem a anotação `Npgsql:ValueGenerationStrategy` de identity;
   - `CK_Product_Price`.
4. A tabela `Stock`, com `CK_Stock_Quantity`, e a tabela `StockMovement`, com `CK_StockMovement_Type`, `CK_StockMovement_Quantity` e `Npgsql:IdentitySequenceOptions` com `'11'`.
5. Os índices:
   - `IX_Product_Name_Trgm`, com `Npgsql:IndexMethod` = `gin`, `Npgsql:IndexOperators` = `gin_trgm_ops` e `filter: "\"IsDeleted\" = false"`;
   - `IX_Product_Category`;
   - `IX_Stock_Quantity`;
   - `IX_StockMovement_ProductId_CreatedAt`, com `descending: new[] { false, true }`.
6. As foreign keys com `onDelete: ReferentialAction.Restrict`.
7. `InsertData` para `Product` (10 linhas, IDs 100001–100010), `Stock` (10) e `StockMovement` (10).
8. Nenhuma coluna `xmin` criada: o `xmin` é uma coluna de sistema do PostgreSQL.

Se o ponto 3 mostrar uma anotação de identity na coluna `Id`, substituir `.UseSequence(IdSequenceName)` por:
```csharp
builder.Property(p => p.Id)
    .HasDefaultValueSql($"nextval('\"{IdSequenceName}\"')")
    .ValueGeneratedOnAdd();
```
Depois, apagar a pasta `Persistence/Migrations` e repetir o Step 16.

- [ ] **Step 18: Aplicar a migration**

Run:
```powershell
dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api
dotnet ef migrations list --project Product/Product.Infrastructure --startup-project Product/Product.Api
```
Expected: `Done.`, e `..._InitialCreate` aparece sem `(Pending)`.

- [ ] **Step 19: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 9: Api — conversão de Result para HTTP e ProductsController (9 endpoints)

Entregável: os 9 endpoints com as rotas, a autorização e os códigos HTTP do spec, testados com um mock do `ISender`.

**Files:**
- Create: `Product/Product.Api/Authorization/AuthorizationRoles.cs`
- Create: `Product/Product.Api/Extensions/ResultExtensions.cs`
- Create: `Product/Product.Api/Controllers/ProductsController.cs`
- Test: `UnitTests/Product.Api.UnitTest/Api/ResultExtensionsTests.cs`, `ProductsControllerTests.cs`, `ProductsControllerAuthorizationTests.cs`

**Interfaces:**
- Consumes: os commands, queries e DTOs (Task 3), o `Result` (Task 2) e o `ISender` do MediatR.
- Produces:
  - `AuthorizationRoles.Admin = "Admin"`;
  - `ResultExtensions`, com:
    - `ToProblem(this ControllerBase, Error) : IActionResult`;
    - `ToActionResult<T>(this Result<T>, ControllerBase, Func<T, IActionResult>)`;
    - `ToActionResult(this Result, ControllerBase, Func<IActionResult>)`;
  - `ProductsController(ISender sender)`, com os 9 métodos públicos do spec.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Api/ResultExtensionsTests.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Product.Api.Controllers;
using Product.Api.Extensions;
using Product.Application.Common.Results;

namespace Product.Api.UnitTest.Api;

public sealed class ResultExtensionsTests
{
    private static ProductsController CreateController() =>
        new(Mock.Of<ISender>()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.BusinessRule, StatusCodes.Status422UnprocessableEntity)]
    public void Error_IsConvertedToProblemDetailsWithMatchingStatus(ErrorType type, int expectedStatus)
    {
        var response = CreateController().ToProblem(new Error("Product.Code", "Something happened.", type));

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("Something happened.", problem.Detail);
        Assert.Equal("Product.Code", problem.Extensions["code"]);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public void ValidationError_IsConvertedToValidationProblemDetails()
    {
        var error = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["'Name' must not be empty."] });

        var response = CreateController().ToProblem(error);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(["'Name' must not be empty."], problem.Errors["Name"]);
    }
}
```

- [ ] **Step 2: Escrever `UnitTests/Product.Api.UnitTest/Api/ProductsControllerTests.cs`**

```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Product.Api.Controllers;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Api;

public sealed class ProductsControllerTests
{
    private static readonly PagedResultDTO<ProductDTO> Page = new([TestProducts.HammerDTO], null, false);

    private readonly Mock<ISender> _sender = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "Admin"), new Claim("role", "Admin")], "Test", "name", "role"));
        _controller = new ProductsController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static int? StatusOf(IActionResult response) => Assert.IsAssignableFrom<IStatusCodeActionResult>(response).StatusCode;

    [Fact]
    public async Task GetProducts_ReturnsOkWithPage()
    {
        _sender.Setup(s => s.Send(It.Is<GetProductsQuery>(q => q.LastItemId == null && q.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.GetProducts(null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task GetProducts_InvalidPaging_ReturnsBadRequest()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Failure(
                Error.Validation(new Dictionary<string, string[]> { ["PageSize"] = ["invalid"] })));

        var response = await _controller.GetProducts(null, 0, Ct);

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(response));
    }

    [Fact]
    public async Task GetProductById_Found_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<GetProductByIdQuery>(q => q.Id == 100001), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(TestProducts.HammerDTO));

        var response = await _controller.GetProductById(100001, Ct);

        Assert.Equal(TestProducts.HammerDTO, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task GetProductById_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Failure(ProductErrors.NotFound(100999)));

        var response = await _controller.GetProductById(100999, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedAtGetProductByIdAndPassesUserName()
    {
        var created = TestProducts.HammerDTO with { Id = 100011 };
        _sender.Setup(s => s.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(created));

        var response = await _controller.CreateProduct(
            new CreateProductDTO("Martelo de Carpinteiro", null, "Ferramentas Manuais", 12.90m, 40), Ct);

        var createdResult = Assert.IsType<CreatedAtActionResult>(response);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(ProductsController.GetProductById), createdResult.ActionName);
        Assert.Equal(100011, createdResult.RouteValues!["id"]);
        Assert.Same(created, createdResult.Value);
        _sender.Verify(s => s.Send(It.Is<CreateProductCommand>(c => c.UserName == "Admin"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<UpdateProductCommand>(c => c.Id == 100001), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(TestProducts.HammerDTO));

        var response = await _controller.UpdateProduct(100001, new UpdateProductDTO("Martelo", null, "Ferramentas Manuais", 12.90m), Ct);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task UpdateProduct_Conflict_ReturnsConflict()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Failure(ProductErrors.ConcurrencyConflict(100001)));

        var response = await _controller.UpdateProduct(100001, new UpdateProductDTO("Martelo", null, "Ferramentas Manuais", 12.90m), Ct);

        Assert.Equal(StatusCodes.Status409Conflict, StatusOf(response));
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNoContentAndPassesUserName()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var response = await _controller.DeleteProduct(100001, Ct);

        Assert.IsType<NoContentResult>(response);
        _sender.Verify(s => s.Send(It.Is<DeleteProductCommand>(c => c.Id == 100001 && c.UserName == "Admin"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ProductErrors.NotFound(100001)));

        var response = await _controller.DeleteProduct(100001, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task DecreaseStockToProduct_ReturnsOkWithStock()
    {
        _sender.Setup(s => s.Send(It.Is<DecreaseStockToProductCommand>(c => c.Id == 100001 && c.Quantity == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Success(new StockDTO(100001, 35)));

        var response = await _controller.DecreaseStockToProduct(100001, 5, Ct);

        Assert.Equal(new StockDTO(100001, 35), Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task DecreaseStockToProduct_InsufficientStock_ReturnsUnprocessableEntity()
    {
        _sender.Setup(s => s.Send(It.IsAny<DecreaseStockToProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Failure(ProductErrors.InsufficientStock(100001, 3, 50)));

        var response = await _controller.DecreaseStockToProduct(100001, 50, Ct);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, StatusOf(response));
    }

    [Fact]
    public async Task AddStockToProduct_ReturnsOkWithStock()
    {
        _sender.Setup(s => s.Send(It.Is<AddStockToProductCommand>(c => c.Id == 100001 && c.Quantity == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Success(new StockDTO(100001, 50)));

        var response = await _controller.AddStockToProduct(100001, 10, Ct);

        Assert.Equal(new StockDTO(100001, 50), Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task AddStockToProduct_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddStockToProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Failure(ProductErrors.NotFound(100999)));

        var response = await _controller.AddStockToProduct(100999, 10, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task SearchProductsByName_ForwardsParametersAndReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<SearchProductsByNameQuery>(q => q.Name == "mart" && q.LastItemId == null && q.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.SearchProductsByName("mart", null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task SearchProductsByStockRange_ForwardsParametersAndReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<SearchProductsByStockRangeQuery>(q => q.Min == 0 && q.Max == 30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.SearchProductsByStockRange(0, 30, null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }
}
```

- [ ] **Step 3: Escrever `UnitTests/Product.Api.UnitTest/Api/ProductsControllerAuthorizationTests.cs`**

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Product.Api.Controllers;

namespace Product.Api.UnitTest.Api;

public sealed class ProductsControllerAuthorizationTests
{
    private static AuthorizeAttribute? AuthorizeOf(string action) =>
        typeof(ProductsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

    [Fact]
    public void Controller_HasNoClassLevelAuthorization() =>
        Assert.Null(typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>());

    [Theory]
    [InlineData(nameof(ProductsController.CreateProduct))]
    [InlineData(nameof(ProductsController.DeleteProduct))]
    public void CreateAndDelete_RequireAdminRole(string action)
    {
        var authorize = AuthorizeOf(action);

        Assert.NotNull(authorize);
        Assert.Equal("Admin", authorize!.Roles);
    }

    [Theory]
    [InlineData(nameof(ProductsController.UpdateProduct))]
    [InlineData(nameof(ProductsController.AddStockToProduct))]
    [InlineData(nameof(ProductsController.DecreaseStockToProduct))]
    public void UpdateAndStock_RequireAuthenticatedUserWithoutRole(string action)
    {
        var authorize = AuthorizeOf(action);

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrEmpty(authorize!.Roles));
    }

    [Theory]
    [InlineData(nameof(ProductsController.GetProducts))]
    [InlineData(nameof(ProductsController.GetProductById))]
    [InlineData(nameof(ProductsController.SearchProductsByName))]
    [InlineData(nameof(ProductsController.SearchProductsByStockRange))]
    public void Reads_ArePublic(string action) => Assert.Null(AuthorizeOf(action));
}
```

- [ ] **Step 4: Correr os testes e confirmar que falham**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~UnitTest.Api"`
Expected: FAIL na compilação (`ProductsController` e `ResultExtensions` não existem).

- [ ] **Step 5: Criar `Product/Product.Api/Authorization/AuthorizationRoles.cs`**

```csharp
namespace Product.Api.Authorization;

public static class AuthorizationRoles
{
    public const string Admin = "Admin";
}
```

- [ ] **Step 6: Criar `Product/Product.Api/Extensions/ResultExtensions.cs`**

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Product.Application.Common.Results;

namespace Product.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : controller.ToProblem(result.Error!);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller, Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : controller.ToProblem(result.Error!);

    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var (status, type) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ErrorType.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "https://tools.ietf.org/html/rfc9110#section-15.5.21"),
            _ => (StatusCodes.Status500InternalServerError, "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        var problem = error.Type == ErrorType.Validation
            ? new ValidationProblemDetails(error.ValidationErrors.ToDictionary(entry => entry.Key, entry => entry.Value))
            {
                Title = error.Message
            }
            : new ProblemDetails
            {
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = error.Message
            };

        problem.Status = status;
        problem.Type = type;
        problem.Instance = controller.HttpContext?.Request.Path;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? controller.HttpContext?.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
```

- [ ] **Step 7: Criar `Product/Product.Api/Controllers/ProductsController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Product.Api.Authorization;
using Product.Api.Extensions;
using Product.Application.Common.DTOs;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;
using Product.Domain.Constants;

namespace Product.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? lastItemId,
        [FromQuery] int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductsQuery(lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, product => Ok(product));
    }

    [HttpPost]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO product, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductCommand(product, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, created => CreatedAtAction(nameof(GetProductById), new { id = created.Id }, created));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDTO product, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProductCommand(id, product), cancellationToken);
        return result.ToActionResult(this, updated => Ok(updated));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, NoContent);
    }

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [Authorize]
    [ProducesResponseType<StockDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DecreaseStockToProduct(int id, int quantity, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DecreaseStockToProductCommand(id, quantity, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, stock => Ok(stock));
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [Authorize]
    [ProducesResponseType<StockDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddStockToProduct(int id, int quantity, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddStockToProductCommand(id, quantity, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, stock => Ok(stock));
    }

    [HttpGet("search")]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProductsByName(
        [FromQuery] string name,
        [FromQuery] int? lastItemId,
        [FromQuery] int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchProductsByNameQuery(name, lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }

    [HttpGet("stock-level")]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProductsByStockRange(
        [FromQuery, BindRequired] int min,
        [FromQuery, BindRequired] int max,
        [FromQuery] int? lastItemId,
        [FromQuery] int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchProductsByStockRangeQuery(min, max, lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }
}
```

- [ ] **Step 8: Correr os testes e confirmar que passam**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~UnitTest.Api"`
Expected: PASS, 29 casos (4 do `ResultExtensions`, 15 do controller e 10 de autorização).

- [ ] **Step 9: Checkpoint**

Run: `dotnet test UnitTests/Product.Api.UnitTest`
Expected: todos os testes passam.

---

### Task 10: Api host — JWT, Swagger com Authorize, exceções globais, migração no arranque, Dockerfile e verificação ponta a ponta

Entregável:
- a Product.Api arranca com o Swagger em `https://localhost:7039/swagger`, aplica as migrations, mostra o cadeado só nos POST, PUT e DELETE e aceita os tokens do App.IdentityServer;
- o script `scripts/smoke-test.ps1` passa;
- a imagem Docker compila.

**Files:**
- Create: `Product/Product.Api/ErrorHandling/GlobalExceptionHandler.cs`
- Create: `Product/Product.Api/Authentication/JwtSettings.cs`, `AuthenticationExtensions.cs`
- Create: `Product/Product.Api/Swagger/AuthorizeOperationFilter.cs`, `SwaggerExtensions.cs`
- Create: `Product/Product.Api/Dockerfile`
- Create: `scripts/smoke-test.ps1`
- Modify: `Product/Product.Api/Program.cs` (versão final), `Product/Product.Api/appsettings.json` (secção `Jwt`)
- Test: `UnitTests/Product.Api.UnitTest/Api/GlobalExceptionHandlerTests.cs`

**Interfaces:**
- Consumes:
  - o `AddApplication()` (Task 4) e o `AddInfrastructure()`/`MigrateProductDatabaseAsync()` (Task 8);
  - o `ProductsController` (Task 9);
  - o login do App.IdentityServer (Plano 01): `iss = https://localhost:7001`, `aud = product.api`, claims `role` e `name`.
- Produces:
  - as variáveis de configuração `Jwt:Authority`, `Jwt:MetadataAddress`, `Jwt:Issuer`, `Jwt:Audience` e `Jwt:RequireHttpsMetadata`, usadas pelo Plano 03;
  - o script `scripts/smoke-test.ps1 -AdminPassword <pwd> -ManagerPassword <pwd> [-IdentityUrl] [-ApiUrl]`.

- [ ] **Step 1: Escrever `UnitTests/Product.Api.UnitTest/Api/GlobalExceptionHandlerTests.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Product.Api.ErrorHandling;

namespace Product.Api.UnitTest.Api;

public sealed class GlobalExceptionHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public async Task UnhandledException_Writes500ProblemDetails(string environmentName, bool expectDetail)
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);
        var handler = new GlobalExceptionHandler(problemDetailsService, environment.Object, NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), Ct);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(problemDetailsService.Written);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetailsService.Written!.ProblemDetails.Status);
        Assert.Equal(expectDetail, problemDetailsService.Written.ProblemDetails.Detail is not null);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }
}
```

- [ ] **Step 2: Correr o teste e confirmar que falha**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~GlobalExceptionHandlerTests"`
Expected: FAIL na compilação (`GlobalExceptionHandler` não existe).

- [ ] **Step 3: Criar `Product/Product.Api/ErrorHandling/GlobalExceptionHandler.cs`**

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Product.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Detail = environment.IsDevelopment() ? exception.ToString() : null
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
```

- [ ] **Step 4: Correr o teste e confirmar que passa**

Run: `dotnet test UnitTests/Product.Api.UnitTest --filter "FullyQualifiedName~GlobalExceptionHandlerTests"`
Expected: PASS, 2 casos.

- [ ] **Step 5: Criar a autenticação JWT**

`Product/Product.Api/Authentication/JwtSettings.cs`:
```csharp
namespace Product.Api.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Authority { get; init; } = string.Empty;
    public string? MetadataAddress { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
}
```

`Product/Product.Api/Authentication/AuthenticationExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Product.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                if (!string.IsNullOrWhiteSpace(settings.MetadataAddress))
                {
                    options.MetadataAddress = settings.MetadataAddress;
                }

                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();
        return services;
    }
}
```

- [ ] **Step 6: Criar o Swagger com o botão Authorize**

`Product/Product.Api/Swagger/AuthorizeOperationFilter.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Product.Api.Swagger;

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public const string SchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        if (authorizeData.Any(data => !string.IsNullOrEmpty(data.Roles)))
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}
```

`Product/Product.Api/Swagger/SwaggerExtensions.cs`:
```csharp
using Microsoft.OpenApi;

namespace Product.Api.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Product API", Version = "v1" });
            options.AddSecurityDefinition(AuthorizeOperationFilter.SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken returned by POST /api/auth/login on App.IdentityServer."
            });
            options.OperationFilter<AuthorizeOperationFilter>();
        });

        return services;
    }
}
```

- [ ] **Step 7: Substituir `Product/Product.Api/appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Authority": "https://localhost:7001",
    "Issuer": "https://localhost:7001",
    "Audience": "product.api",
    "RequireHttpsMetadata": true
  }
}
```

- [ ] **Step 8: Substituir `Product/Product.Api/Program.cs` (versão final)**

```csharp
using System.Diagnostics;
using Product.Api.Authentication;
using Product.Api.ErrorHandling;
using Product.Api.Swagger;
using Product.Application;
using Product.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouse")
    ?? throw new InvalidOperationException("Connection string 'StoreHouse' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

await app.Services.MigrateProductDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 9: Criar `Product/Product.Api/Dockerfile`**

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
COPY ["Product/Product.Api/Product.Api.csproj", "Product/Product.Api/"]
COPY ["Product/Product.Application/Product.Application.csproj", "Product/Product.Application/"]
COPY ["Product/Product.Domain/Product.Domain.csproj", "Product/Product.Domain/"]
COPY ["Product/Product.Infrastructure/Product.Infrastructure.csproj", "Product/Product.Infrastructure/"]
RUN dotnet restore "Product/Product.Api/Product.Api.csproj"
COPY ["Product/", "Product/"]
WORKDIR /src/Product/Product.Api
RUN dotnet publish "Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.Api.dll"]
```

- [ ] **Step 10: Criar `scripts/smoke-test.ps1`**

Funciona em Windows PowerShell 5.1 e em PowerShell 7. As passwords são passadas como parâmetros e nunca ficam no ficheiro.

```powershell
param(
    [string]$IdentityUrl = 'https://localhost:7001',
    [string]$ApiUrl = 'https://localhost:7039',
    [Parameter(Mandatory = $true)][string]$AdminPassword,
    [Parameter(Mandatory = $true)][string]$ManagerPassword
)

$ErrorActionPreference = 'Stop'

function Get-AccessToken([string]$UserName, [string]$Password) {
    $body = @{ userName = $UserName; password = $Password } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "$IdentityUrl/api/auth/login" -ContentType 'application/json' -Body $body).accessToken
}

function Invoke-Api([string]$Method, [string]$Path, [string]$Token = $null, $Body = $null) {
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    $request = @{ Method = $Method; Uri = "$ApiUrl$Path"; Headers = $headers; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $request['ContentType'] = 'application/json'
        $request['Body'] = $Body | ConvertTo-Json
    }
    try {
        $response = Invoke-WebRequest @request
        $content = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $content }
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        return [pscustomobject]@{ Status = [int]$_.Exception.Response.StatusCode; Body = $null }
    }
}

function Assert-Status($Result, [int]$Expected, [string]$Step) {
    if ($Result.Status -ne $Expected) { throw "FALHOU: $Step (esperado $Expected, obtido $($Result.Status))" }
    Write-Host "OK  $Step -> $Expected"
}

function Assert-True([bool]$Condition, [string]$Step) {
    if (-not $Condition) { throw "FALHOU: $Step" }
    Write-Host "OK  $Step"
}

$page1 = Invoke-Api GET '/api/products'
Assert-Status $page1 200 'GET /api/products'
Assert-True ($page1.Body.items.Count -eq 5 -and $page1.Body.hasMore -and $page1.Body.nextCursor -eq 100005) 'primeira página com 5 produtos e cursor 100005'

$page2 = Invoke-Api GET "/api/products?lastItemId=$($page1.Body.nextCursor)"
Assert-Status $page2 200 'GET /api/products?lastItemId=100005'
Assert-True ($page2.Body.items.Count -ge 5 -and $page2.Body.items[0].id -eq 100006) 'segunda página começa em 100006'

Assert-Status (Invoke-Api GET '/api/products/100001') 200 'GET /api/products/100001'
Assert-Status (Invoke-Api GET '/api/products/99999') 400 'GET /api/products/99999 (id inválido)'

$search = Invoke-Api GET '/api/products/search?name=mart'
Assert-Status $search 200 'GET /api/products/search?name=mart'
Assert-True ($search.Body.items[0].name -eq 'Martelo de Carpinteiro') 'pesquisa parcial encontra o Martelo'

$range = Invoke-Api GET '/api/products/stock-level?min=0&max=30'
Assert-Status $range 200 'GET /api/products/stock-level?min=0&max=30'
Assert-True (@($range.Body.items | ForEach-Object { $_.id }) -contains 100002) 'intervalo de stock inclui o Berbequim (15)'

$newProduct = @{ name = 'Produto de Teste'; description = 'Criado pelo smoke test'; category = 'Testes'; price = 4.99; initialStock = 10 }
Assert-Status (Invoke-Api POST '/api/products' $null $newProduct) 401 'POST sem token'

$managerToken = Get-AccessToken 'Manager' $ManagerPassword
$adminToken = Get-AccessToken 'Admin' $AdminPassword

Assert-Status (Invoke-Api POST '/api/products' $managerToken $newProduct) 403 'POST com Manager'
Assert-Status (Invoke-Api POST '/api/products' $adminToken @{ name = ''; category = ''; price = 0; initialStock = -1 }) 400 'POST inválido'

$created = Invoke-Api POST '/api/products' $adminToken $newProduct
Assert-Status $created 201 'POST com Admin'
$id = $created.Body.id
Assert-True ($id -ge 100011 -and $id -le 999999 -and $created.Body.stockQuantity -eq 10) "id gerado ($id) com 6 dígitos e stock 10"

$update = @{ name = 'Produto de Teste Alterado'; description = $null; category = 'Testes'; price = 5.49 }
Assert-Status (Invoke-Api PUT "/api/products/$id" $managerToken $update) 200 'PUT com Manager'

$added = Invoke-Api POST "/api/products/$id/add-to-stock/5" $managerToken
Assert-Status $added 200 'add-to-stock 5'
Assert-True ($added.Body.quantity -eq 15) 'stock passou a 15'

Assert-Status (Invoke-Api POST "/api/products/$id/decrement-stock/100" $managerToken) 422 'decrement-stock acima do disponível'
$decreased = Invoke-Api POST "/api/products/$id/decrement-stock/5" $managerToken
Assert-Status $decreased 200 'decrement-stock 5'
Assert-True ($decreased.Body.quantity -eq 10) 'stock voltou a 10'

Assert-Status (Invoke-Api DELETE "/api/products/$id" $managerToken) 403 'DELETE com Manager'
Assert-Status (Invoke-Api DELETE "/api/products/$id" $adminToken) 204 'DELETE com Admin (soft delete)'
Assert-Status (Invoke-Api GET "/api/products/$id") 404 'GET depois do DELETE'
Assert-Status (Invoke-Api DELETE "/api/products/$id" $adminToken) 404 'DELETE repetido'

Write-Host "`nSmoke test concluído com sucesso." -ForegroundColor Green
```

- [ ] **Step 11: Compilar e correr todos os testes**

Run:
```powershell
dotnet build StoreHouse.slnx
dotnet test UnitTests/Product.Api.UnitTest
```
Expected: `Build succeeded`, e todos os testes passam, com 0 falhas.

- [ ] **Step 12: Arrancar os dois serviços**

Run, em terminais separados:
```powershell
# terminal 1
dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
# terminal 2
dotnet run --project Product/Product.Api --launch-profile https
```
Expected: o terminal 2 mostra `Now listening on: https://localhost:7039`, sem erros de migração.

- [ ] **Step 13: Verificar o Swagger**

Abrir `https://localhost:7039/swagger` e confirmar:
- aparecem os 9 endpoints;
- o cadeado aparece só em `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}`, `POST .../decrement-stock/{quantity}` e `POST .../add-to-stock/{quantity}`;
- os schemas `ProductDTO`, `CreateProductDTO`, `UpdateProductDTO`, `StockDTO` e `ProductDTOPagedResultDTO` (ou equivalente) estão presentes.

Fazer login no Swagger do IdentityServer (`https://localhost:7001/swagger`, `POST /api/auth/login`), copiar o `accessToken`, carregar em **Authorize** no Swagger da Product.Api, colar o token e executar o `POST /api/products`.
Expected: `201` com o Admin e `403` com o Manager.

- [ ] **Step 14: Correr o smoke test**

Run (terminal 3):
```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword 'nebur#123' -ManagerPassword 'nebur2#123'
```
Expected: todas as linhas começam por `OK`, e a última é `Smoke test concluído com sucesso.`

- [ ] **Step 15: Compilar a imagem Docker**

Run: `docker build -f Product/Product.Api/Dockerfile -t storehouse/product-api:dev .`
Expected: `naming to docker.io/storehouse/product-api:dev`, sem erros.

- [ ] **Step 16: Checkpoint final do Plano 02**

Parar os serviços com `Ctrl+C`.

Run: `dotnet test StoreHouse.slnx`
Expected: todos os testes passam.

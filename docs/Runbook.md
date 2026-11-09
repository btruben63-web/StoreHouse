# Runbook

Guia operacional curto. Passos de utilização da API (obter token, endpoints, utilizadores de teste) estão no [README](../README.md); arquitetura em [Architecture.md](Architecture.md). Todos os comandos correm na raiz do repositório, em PowerShell.

## 1. Pré-requisitos

| Necessário para | Requisito |
|---|---|
| Tudo | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (verificado com 10.0.401) |
| Execução local | PostgreSQL em `localhost:5432` (o README indica 18.x) |
| Execução em Docker | Docker com Compose v2 (verificado com Docker 29.9.2) |
| Migrations manuais (opcional) | `dotnet tool install --global dotnet-ef --version 10.0.13` |
| HTTPS local | `dotnet dev-certs https --trust` |

## 2. Configuração

Os valores são lidos de `appsettings*.json`, User Secrets ou variáveis de ambiente (`:` passa a `__`, por exemplo `ConnectionStrings__StoreHouse`).

**App.IdentityServer**

| Chave | Obrigatória | Valor por omissão (`appsettings.json`) |
|---|---|---|
| `ConnectionStrings:StoreHouseIdentity` | Sim | — (User Secrets / env) |
| `IdentityServer:ClientSecret` | Sim | — (User Secrets / env) |
| `IdentityServer:IssuerUri` | Sim | `https://localhost:7001` |
| `IdentityServer:TokenEndpoint` | Sim | `https://localhost:7001/connect/token` |
| `IdentityServer:ClientId` | Sim | `storehouse.swagger` |
| `IdentityServer:ApiScope` | Sim | `product.api` |
| `IdentityServer:AccessTokenLifetimeSeconds` | Não | `3600` |
| `IdentityServer:KeyPath` | Não | não definido (o Duende usa o seu caminho por omissão; no Compose é `/home/app/keys`) |

**Product.Api**

| Chave | Obrigatória | Valor por omissão (`appsettings.json`) |
|---|---|---|
| `ConnectionStrings:StoreHouse` | Sim | — (User Secrets / env) |
| `Jwt:Authority` | Sim | `https://localhost:7001` |
| `Jwt:Issuer` | Sim | `https://localhost:7001` (tem de coincidir com `IdentityServer:IssuerUri`) |
| `Jwt:Audience` | Sim | `product.api` (tem de coincidir com `IdentityServer:ApiScope`) |
| `Jwt:RequireHttpsMetadata` | Não | `true` |
| `Jwt:MetadataAddress` | Não | não definido |

**Docker Compose (`.env`, criado a partir de `.env.example`; o ficheiro está no `.gitignore`)**

`POSTGRES_USER`, `POSTGRES_PASSWORD`, `CERT_PASSWORD`, `IDENTITY_CLIENT_SECRET`. O Compose injeta-os nas connection strings, no certificado Kestrel e em `IdentityServer__ClientSecret`.

## 3. Instalar e executar

### Local

1. Confiar no certificado de desenvolvimento: `dotnet dev-certs https --trust`.
2. Definir os segredos (comandos completos no [README](../README.md#correr-localmente-visual-studio-ou-cli)): `ConnectionStrings:StoreHouseIdentity`, `IdentityServer:ClientSecret` (projeto `IdentityServer/App.IdentityServer`) e `ConnectionStrings:StoreHouse` (projeto `Product/Product.Api`), com `dotnet user-secrets set`.
3. Arrancar em dois terminais:

```powershell
dotnet run --project IdentityServer/App.IdentityServer --launch-profile https
dotnet run --project Product/Product.Api --launch-profile https
```

As BDs `StoreHouseIdentity` e `StoreHouse` são criadas e populadas pelas migrations no arranque.

### Docker

```powershell
Copy-Item .env.example .env          # preencher os valores
New-Item -ItemType Directory -Force certs | Out-Null
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p "<CERT_PASSWORD>"
dotnet dev-certs https --trust
docker compose up --build -d
docker compose ps
```

### Endereços

| Serviço | Local | Docker (host) |
|---|---|---|
| App.IdentityServer | `https://localhost:7001/swagger` (HTTP: `http://localhost:5001`) | `https://localhost:7001/swagger` |
| Product.Api | `https://localhost:7039/swagger` (HTTP: `http://localhost:5039`) | `https://localhost:7039/swagger` |
| PostgreSQL | `localhost:5432` | `localhost:5433` |

O Swagger só está ativo com `ASPNETCORE_ENVIRONMENT=Development` (definido nos perfis `https` e no Compose).

## 4. Testes e validações

| Objetivo | Comando |
|---|---|
| Compilar a solução | `dotnet build` |
| Testes unitários | `dotnet test` (139 testes passaram na última verificação) |
| Verificação ponta a ponta (apps a correr) | `powershell -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -AdminPassword '<password-Admin>' -ManagerPassword '<password-Manager>'` |
| Validar o Compose | `docker compose config -q` |

Passwords dos utilizadores de demonstração: ver [README](../README.md#utilizadores-de-teste). O smoke test aceita ainda `-IdentityUrl` (por omissão `https://localhost:7001`) e `-ApiUrl` (por omissão `https://localhost:7039`) e assume os dados de seed, por isso falha se os produtos de seed tiverem sido alterados.

## 5. Comandos úteis

```powershell
# Migrations sem arrancar as aplicações
dotnet ef database update --project IdentityServer/App.IdentityServer
dotnet ef database update --project Product/Product.Infrastructure --startup-project Product/Product.Api

# Docker
docker compose logs -f product-api         # serviços: postgres, app-identityserver, product-api
docker compose restart product-api
docker compose down                        # parar
docker compose down -v                     # parar e apagar dados (BD e chaves de assinatura)
```

Os hashes das passwords de seed estão pré-calculados em `IdentitySeedData.cs`; se a password de seed mudar, têm de ser regenerados (nota no próprio ficheiro).

## 6. Problemas conhecidos

- Em Docker as apps correm com `ASPNETCORE_ENVIRONMENT=Development`: Swagger ativo e respostas `500` com detalhe da exceção. Uso apenas para demonstração.
- Se a ligação à BD cair após um commit de stock, a *execution strategy* pode repetir a operação (sem chave de idempotência).
- O Duende IdentityServer exige licença em produção; sem ela o log mostra avisos.
- Não há testes de integração nem cobertura de persistência/migrations: falhas nessa camada só se detetam a correr as aplicações.

## 7. Troubleshooting

| Sintoma | Causa provável | Ação |
|---|---|---|
| Arranque falha com `Connection string '...' is not configured.` | Segredo/variável em falta | Definir `ConnectionStrings:StoreHouse` (API) ou `ConnectionStrings:StoreHouseIdentity` (IdentityServer) |
| IdentityServer falha com `Configuration value 'IdentityServer:...' is required.` | Chave obrigatória vazia (tipicamente `ClientSecret`) | Definir o segredo (User Secrets ou `IDENTITY_CLIENT_SECRET`) |
| `POST /api/auth/login` devolve `500` | O login chama o `TokenEndpoint` por HTTPS; qualquer erro que não seja credenciais inválidas lança exceção (por exemplo certificado não confiável ou endpoint inacessível) | `dotnet dev-certs https --trust`; confirmar `IdentityServer:TokenEndpoint` |
| `POST /api/auth/login` devolve `401` | Credenciais inválidas (`invalid_grant` do token endpoint). O lockout (5 falhas, 5 minutos) está configurado no Identity; o código não o trata em separado | Verificar utilizador/password; aguardar o fim do lockout |
| Product.Api devolve `401` com token válido | `Jwt:Issuer`/`Jwt:Audience` diferentes de `IdentityServer:IssuerUri`/`ApiScope`; token expirado (1 h); `Authority`/`MetadataAddress` inacessíveis a partir da API | Alinhar os valores; pedir novo token; em Docker a API usa `http://app-identityserver:8080` |
| `403` em `POST`/`DELETE` | Só o role `Admin` | Usar um token de `Admin` |
| `400` em `/{id}` ou `lastItemId` | IDs válidos: 100000–999999; `pageSize` 1–50; `name` com 2–100 caracteres; `min` ≥ 0 e `max` ≥ `min` | Corrigir os parâmetros |
| `409` em `PUT`/`DELETE` | Produto alterado por outro pedido (`xmin`) | Reler o produto e repetir |
| `422` em stock | Stock insuficiente (`decrement-stock`) ou limite de stock excedido (`add-to-stock`); quantidade por operação: 1–1 000 000 | Ajustar a quantidade |
| `docker compose` responde `no configuration file provided: not found` | Comando fora da raiz | Correr na pasta do `docker-compose.yml` |
| Contentores não arrancam por portas ocupadas | Portas 7001, 7039 ou 5433 em uso | Parar a instância local ou o processo que as ocupa |
| Erro de certificado nos contentores | `certs/aspnetapp.pfx` em falta ou `CERT_PASSWORD` diferente da usada ao exportar | Voltar a exportar o certificado com a mesma password |
| Mudar `POSTGRES_USER`/`POSTGRES_PASSWORD` não tem efeito | O volume `pgdata` mantém as credenciais da primeira inicialização (comportamento da imagem `postgres`) | `docker compose down -v` (apaga os dados) |
| `/swagger` devolve `404` | Ambiente diferente de `Development` | Definir `ASPNETCORE_ENVIRONMENT=Development` |

# Changelog

Formato inspirado em [Keep a Changelog](https://keepachangelog.com/pt-PT/1.1.0/).

**Estado do versionamento:** o repositório tem um único commit (`eccc045`, 2026-11-09, "First commit"), sem tags nem número de versão definido no código. Por isso não existem versões nem alterações anteriores documentáveis; tudo o que está abaixo pertence à primeira entrega. Datas só são indicadas quando constam do repositório.

## [Não versionado] — 2026-11-09

### Adicionado

**Product.Api**
- 9 endpoints em `/api/products`: listar, obter por id, criar, atualizar, apagar, aumentar stock, diminuir stock, pesquisar por nome e pesquisar por intervalo de stock.
- Paginação por cursor (*keyset*) nas listagens e pesquisas.
- Autenticação JWT Bearer e autorização por role (`Admin` em `POST` e `DELETE`).
- Erros em `ProblemDetails` com `code` e `traceId`; tratamento global de exceções.
- Swagger com esquema Bearer (apenas em `Development`).

**App.IdentityServer**
- `POST /api/auth/login` sobre Duende IdentityServer + ASP.NET Identity (utilizadores `Admin` e `Manager` de seed, lockout após 5 falhas).

**Persistência**
- Migrations iniciais: `InitialIdentity` (2026-11-09) para o IdentityServer e `InitialCreate` (2026-11-09) para os produtos; ambas aplicadas no arranque.
- Seed de 10 produtos com stock e movimentos iniciais.
- Registo de movimentos de stock (`StockMovement`) e soft delete de produtos.

**Operação e qualidade**
- Dockerfiles, `docker-compose.yml` (Postgres, IdentityServer, Product.Api) e `.env.example`.
- `scripts/smoke-test.ps1` para verificação ponta a ponta.
- 139 testes unitários (xUnit v3 + Moq) sobre API, handlers, validadores, behaviors, mapeamento e tratamento de erros.
- README com instruções de execução, e esta documentação em `docs/`.

### Arquitetura

Arquitetura inicial: Clean Architecture + CQRS em `Product.*`, IdentityServer separado, uma BD PostgreSQL por serviço. Descrição em [Architecture.md](Architecture.md). Não há alterações arquiteturais registadas após a primeira entrega.

### Corrigido / Melhorado

Sem informação disponível: não há histórico de correções nem de melhorias no repositório.

### Breaking changes

Nenhuma identificável: não existe versão anterior.
